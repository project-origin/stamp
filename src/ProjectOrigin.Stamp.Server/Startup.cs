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
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.Stamp.Server.BackgroundServices;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Database.Mapping;
using ProjectOrigin.Stamp.Server.Database.Postgres;
using ProjectOrigin.Stamp.Server.Extensions;
using ProjectOrigin.Stamp.Server.Options;
using ProjectOrigin.Stamp.Server.Serialization;
using ProjectOrigin.Stamp.Server.Services.REST;
using ProjectOrigin.Stamp.Server.EventHandlers;
using ProjectOrigin.Stamp.Server.Helpers;
using ProjectOrigin.Stamp.Server.Metrics;

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

        services.AddHealthChecks();

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
        services.AddSingleton<MeterBase>();
        services.AddSingleton<IStampMetrics, StampMetrics>();

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
                    .AddMeter(MeterBase.MeterName)
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(o => o.Endpoint = otlpOptions.Endpoint!))
                .WithTracing(provider =>
                    provider
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddNpgsql()
                        .AddSource(DiagnosticHeaders.DefaultListenerName)
                        .AddOtlpExporter(o => o.Endpoint = otlpOptions.Endpoint!));
        }

        services.AddMassTransit(o =>
        {
            o.DisableUsageTelemetry(); // https://masstransit.io/documentation/configuration/usage-telemetry
            o.AddHealthChecks();
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
            endpoints.MapHealthChecks("/health");
        });

        app.ConfigureSqlMappers();
    }
}
