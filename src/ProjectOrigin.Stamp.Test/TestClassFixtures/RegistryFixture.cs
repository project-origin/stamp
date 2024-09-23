using System.Text;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace ProjectOrigin.Stamp.Test.TestClassFixtures;

public class RegistryFixture : IAsyncLifetime
{
    private const string registryImage = "ghcr.io/project-origin/registry-server:2.0.0";
    private const string electricityVerifierImage = "ghcr.io/project-origin/electricity-server:1.2.0";
    protected const int GrpcPort = 5000;
    private const int RabbitMqHttpPort = 15672;
    private const string registryName = "TestRegistry";
    private const string RegistryAlias = "registry-container";
    private const string VerifierAlias = "verifier-container";
    private const string RabbitMqAlias = "rabbitmq-container";

    private readonly Lazy<IContainer> registryContainer;
    private readonly IContainer verifierContainer;
    private readonly Testcontainers.RabbitMq.RabbitMqContainer rabbitMqContainer;
    private readonly PostgreSqlContainer registryPostgresContainer;
    protected readonly INetwork Network;
    private readonly IFutureDockerImage rabbitMqImage;

    public const string RegistryName = registryName;
    public IPrivateKey Dk1IssuerKey { get; init; }
    public IPrivateKey Dk2IssuerKey { get; init; }
    public string RegistryUrl => $"http://{registryContainer.Value.Hostname}:{registryContainer.Value.GetMappedPublicPort(GrpcPort)}";
    protected string RegistryContainerUrl => $"http://{registryContainer.Value.IpAddress}:{GrpcPort}";

    public RegistryFixture()
    {
        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString())
            .Build();

        rabbitMqImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetProjectDirectory(), string.Empty)
            .WithDockerfile("rabbitmq.dockerfile")
            .Build();

        rabbitMqContainer = new RabbitMqBuilder()
            .WithImage(rabbitMqImage)
            .WithNetwork(Network)
            .WithNetworkAliases(RabbitMqAlias)
            .WithPortBinding(RabbitMqHttpPort, true)
            .Build();

        Dk1IssuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();
        Dk2IssuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        var configFile = Path.GetTempFileName() + ".yaml";
        File.WriteAllText(configFile, $"""
        registries:
          {registryName}:
            url: http://{RegistryAlias}:{GrpcPort}
        areas:
          DK1:
            issuerKeys:
              - publicKey: "{Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk1IssuerKey.PublicKey.ExportPkixText()))}"
          DK2:
            issuerKeys:
              - publicKey: "{Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk2IssuerKey.PublicKey.ExportPkixText()))}"
        """);

        var waitForLogRegex = new Regex(@"Application started*.");

        verifierContainer = new ContainerBuilder()
                .WithImage(electricityVerifierImage)
                .WithNetwork(Network)
                .WithNetworkAliases(VerifierAlias)
                .WithResourceMapping(configFile, "/app/tmp/")
                .WithPortBinding(GrpcPort, true)
                .WithCommand("--serve")
                .WithEnvironment("Network__ConfigurationUri", "file:///app/tmp/" + Path.GetFileName(configFile))
                .Build();

        registryPostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .Build();

        registryContainer = new Lazy<IContainer>(() => new ContainerBuilder()
            .WithImage(registryImage)
            .WithNetwork(Network)
            .WithNetworkAliases(RegistryAlias)
            .WithPortBinding(GrpcPort, true)
            .WithCommand("--migrate", "--serve")
            .WithEnvironment("RegistryName", registryName)
            .WithEnvironment("Otlp__Enabled", "false")
            .WithEnvironment("Verifiers__project_origin.electricity.v1", $"http://{VerifierAlias}:{GrpcPort}")
            .WithEnvironment("IMMUTABLELOG__TYPE", "log")
            .WithEnvironment("BlockFinalizer__Interval", "00:00:05")
            .WithEnvironment("cache__TYPE", "InMemory")
            .WithEnvironment("RabbitMq__Hostname", RabbitMqAlias)
            .WithEnvironment("RabbitMq__AmqpPort", RabbitMqBuilder.RabbitMqPort.ToString())
            .WithEnvironment("RabbitMq__HttpApiPort", RabbitMqHttpPort.ToString())
            .WithEnvironment("RabbitMq__Username", RabbitMqBuilder.DefaultUsername)
            .WithEnvironment("RabbitMq__Password", RabbitMqBuilder.DefaultPassword)
            .WithEnvironment("TransactionProcessor__ServerNumber", "0")
            .WithEnvironment("TransactionProcessor__Servers", "1")
            .WithEnvironment("TransactionProcessor__Threads", "5")
            .WithEnvironment("TransactionProcessor__Weight", "10")
            .WithEnvironment("ConnectionStrings__Database", registryPostgresContainer.GetConnectionString())
            .Build()
        );
    }

    public virtual async Task InitializeAsync()
    {
        await rabbitMqImage.CreateAsync();
        await Network.CreateAsync();
        await rabbitMqContainer.StartAsync();
        await verifierContainer.StartAsync();
        await registryPostgresContainer.StartAsync();
        await registryContainer.Value.StartAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await registryContainer.Value.StopAsync();
        await registryPostgresContainer.StopAsync();
        await rabbitMqContainer.StopAsync();
        await verifierContainer.StopAsync();
        await Network.DisposeAsync();
    }
}
