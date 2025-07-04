using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Stamp.Server.Database;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;

namespace ProjectOrigin.Stamp.Server.Extensions;

public static class IConfigurationExtensions
{
    public static T GetValid<T>(this IConfiguration configuration) where T : IValidatableObject
    {
        try
        {
            var value = configuration.Get<T>();

            if (value is null)
                throw new ArgumentNullException($"Configuration value of type {typeof(T)} is null");

            Validator.ValidateObject(value, new ValidationContext(value), true);

            return value;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to convert configuration value"))
        {
            throw new ValidationException($"Configuration value of type {typeof(T)} is invalid", ex);
        }
    }

    public static IRepositoryUpgrader GetRepositoryUpgrader(this IConfiguration configuration, Serilog.ILogger logger)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSerilog(logger);
        services.ConfigurePersistance(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IRepositoryUpgrader>();
    }

    public static WebApplication BuildApp(this IConfigurationRoot configuration)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Configuration.Sources.Clear();
        builder.Configuration.AddConfiguration(configuration, shouldDisposeConfiguration: true);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);

        var startup = new Startup(builder.Configuration);
        startup.ConfigureServices(builder.Services);

        var app = builder.Build();
        startup.Configure(app, builder.Environment);
        return app;
    }

    public static Serilog.ILogger GetSeriLogger(this IConfiguration configuration)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'")
            .Filter.ByExcluding("SourceContext = 'MassTransit.ReceiveTransport' and MessageTemplate like 'R-RETRY%'")
            .Filter.ByExcluding(logEvent =>
                logEvent.MessageTemplate.Text.Contains("R-RETRY") &&
                logEvent.MessageTemplate.Text.Contains("MassTransit.ReceiveTransport"))
            .Enrich.WithSpan();

        var logOutputFormat = configuration.GetValue<string>("LogOutputFormat");

        switch (logOutputFormat)
        {
            case "json":
                loggerConfiguration = loggerConfiguration.WriteTo.Console(new JsonFormatter());
                break;

            case "text":
                loggerConfiguration = loggerConfiguration.WriteTo.Console();
                break;

            default:
                throw new NotSupportedException($"LogOutputFormat of value ”{logOutputFormat}” is not supported");
        }

        return loggerConfiguration.CreateLogger();
    }
}
