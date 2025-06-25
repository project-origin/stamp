using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Protocols.Configuration;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Database.Postgres;

namespace ProjectOrigin.Stamp.Server.Extensions;

public static class IServiceCollectionExtensions
{
    private static readonly string[] PostgresHealthCheckTags = ["ready", "db"];

    public static void ConfigurePersistance(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IRepositoryUpgrader, PostgresUpgrader>();
        services.AddOptions<PostgresOptions>()
            .Configure(x => x.ConnectionString = configuration.GetConnectionString("Database")
                ?? throw new InvalidConfigurationException("Configuration does not contain a connection string named 'Database'."))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var healthChecks = services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());
        healthChecks.AddNpgSql(configuration.GetConnectionString("Database") ?? throw new InvalidOperationException(), name: "postgres", tags: PostgresHealthCheckTags);
    }
}
