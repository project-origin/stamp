using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MassTransit;
using MassTransit.Logging;
using MassTransit.Monitoring;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.Stamp.BackgroundServices;
using ProjectOrigin.Stamp.Database;
using ProjectOrigin.Stamp.Database.Mapping;
using ProjectOrigin.Stamp.Database.Postgres;
using ProjectOrigin.Stamp.Extensions;
using ProjectOrigin.Stamp.Options;
using ProjectOrigin.Stamp.Serialization;
using ProjectOrigin.Stamp.Services.REST;
using ProjectOrigin.Stamp.EventHandlers;
using ProjectOrigin.Stamp.Helpers;

namespace ProjectOrigin.Stamp;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc();

        var algorithm = new Secp256k1Algorithm();
        services.AddSingleton<IHDAlgorithm>(algorithm);

        services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                o.JsonSerializerOptions.Converters.Add(new IHDPublicKeyConverter(algorithm));
            });

        services.AddSwaggerGen(o =>
        {
            o.SupportNonNullableReferenceTypes();
            o.DocumentFilter<PathBaseDocumentFilter>();
        });

        services.AddOptions<RestApiOptions>()
            .Bind(_configuration.GetSection("RestApiOptions"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OtlpOptions>()
            .BindConfiguration(OtlpOptions.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RetryOptions>()
            .BindConfiguration(RetryOptions.Retry)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RegistryOptions>()
            .Bind(_configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient();

        services.ConfigurePersistance(_configuration);

        var otlpOptions = _configuration.GetSection(OtlpOptions.Prefix).GetValid<OtlpOptions>();
        if (otlpOptions.Enabled)
        {
            services.AddOpenTelemetry()
                .ConfigureResource(r =>
                {
                    r.AddService("ProjectOrigin.Stamp",
                    serviceInstanceId: Environment.MachineName);
                })
                .WithMetrics(metrics => metrics
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddMeter(InstrumentationOptions.MeterName)
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter(o => o.Endpoint = otlpOptions.Endpoint!))
                .WithTracing(provider =>
                    provider
                        .AddGrpcClientInstrumentation(grpcOptions =>
                        {
                            grpcOptions.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                                activity.SetTag("requestVersion", httpRequestMessage.Version);
                            grpcOptions.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                                activity.SetTag("responseVersion", httpResponseMessage.Version);
                            grpcOptions.SuppressDownstreamInstrumentation = true;
                        })
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddNpgsql()
                        .AddSource(DiagnosticHeaders.DefaultListenerName)
                        .AddOtlpExporter(o => o.Endpoint = otlpOptions.Endpoint!));
        }

        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            o.AddConfigureEndpointsCallback((name, cfg) =>
            {
                if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
                    rmq.SetQuorumQueue(3);
            });

            o.AddConsumer<IssueInRegistryConsumer, IssueInRegistryConsumerDefinition>();
            o.AddConsumer<RejectCertificateConsumer, RejectCertificateConsumerDefinition>();
            o.AddConsumer<MarkCertificateAsIssuedConsumer, MarkCertificateAsIssuedConsumerDefinition>();
            o.AddConsumer<SendToWalletConsumer, SendToWalletConsumerDefinition>();
            o.AddConsumer<WaitForCommittedRegistryTransactionConsumer, WaitForCommittedRegistryTransactionConsumerDefinition>();

            o.ConfigureMassTransitTransport(_configuration.GetSection("MessageBroker").GetValid<MessageBrokerOptions>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();
        services.AddScoped<IKeyGenerator, KeyGenerator>();

        services.AddHostedService<OutboxPollingWorker>();

        services.AddSwaggerGen(options =>
        {
            options.EnableAnnotations();
            options.SchemaFilter<IHDPublicKeySchemaFilter>();
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);
            options.DocumentFilter<AddStampTagDocumentFilter>();
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var pathBase = app.ApplicationServices.GetRequiredService<IOptions<RestApiOptions>>().Value.PathBase;
        app.UsePathBase(pathBase);

        app.UseSwagger();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        app.ConfigureSqlMappers();
    }
}
