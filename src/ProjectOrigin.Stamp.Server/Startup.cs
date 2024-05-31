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
using ProjectOrigin.Stamp.Server.BackgroundServices;
using ProjectOrigin.Stamp.Server.CommandHandlers;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Database.Mapping;
using ProjectOrigin.Stamp.Server.Database.Postgres;
using ProjectOrigin.Stamp.Server.Extensions;
using ProjectOrigin.Stamp.Server.Options;
using ProjectOrigin.Stamp.Server.Serialization;
using ProjectOrigin.Stamp.Server.Services.REST;
using ProjectOrigin.Stamp.Server.EventHandlers;

namespace ProjectOrigin.Stamp.Server;

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

        services.AddRegistryOptions();

        services.ConfigurePersistance(_configuration);

        var otlpOptions = _configuration.GetSection(OtlpOptions.Prefix).GetValid<OtlpOptions>();
        if (otlpOptions.Enabled)
        {
            services.AddOpenTelemetry()
                .ConfigureResource(r =>
                {
                    r.AddService("ProjectOrigin.Stamp.Server",
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

            o.AddConsumer<CreateCertificateCommandHandler, CreateCertificateCommandHandlerDefinition>();
            o.AddConsumer<CertificateCreatedEventHandler, CertificateCreatedEventHandlerDefinition>();
            o.AddConsumer<CertificateFailedInRegistryEventHandler, CertificateFailedInRegistryEventHandlerDefinition>();
            o.AddConsumer<CertificateIssuedInRegistryEventHandler, CertificateIssuedInRegistryEventHandlerDefinition>();
            o.AddConsumer<CertificateMarkedAsIssuedEventHandler, CertificateMarkedAsIssuedEventHandlerDefinition>();
            o.AddConsumer<CertificateSentToRegistryEventHandler, CertificateSentToRegistryEventHandlerDefinition>();

            o.ConfigureMassTransitTransport(_configuration.GetSection("MessageBroker").GetValid<MessageBrokerOptions>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();

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
