using System.Text;
using DotNet.Testcontainers.Builders;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.Stamp.Test.Extensions;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using Xunit;
using Xunit.Abstractions;

namespace ProjectOrigin.Stamp.Test;

public class TestContainerTests
{
    private readonly ITestOutputHelper outputHelper;
    private const string electricityVerifierImage = "ghcr.io/project-origin/electricity-server:1.2.0";
    protected const int GrpcPort = 5000;
    private const string registryName = "TestRegistry";
    private const string RegistryAlias = "registry-container";

    public TestContainerTests(ITestOutputHelper outputHelper)
    {
        this.outputHelper = outputHelper;
    }

    [Fact]
    public async Task TestContainer()
    {
        var configFile = Path.GetTempFileName() + ".yaml";
        File.WriteAllText(configFile, $"""
        registries:
          {registryName}:
            url: http://{RegistryAlias}:{GrpcPort}
        areas:
          DK1:
            issuerKeys:
              - publicKey: "{Convert.ToBase64String(Encoding.UTF8.GetBytes(Algorithms.Ed25519.GenerateNewPrivateKey().PublicKey.ExportPkixText()))}"
          DK2:
            issuerKeys:
              - publicKey: "{Convert.ToBase64String(Encoding.UTF8.GetBytes(Algorithms.Ed25519.GenerateNewPrivateKey().PublicKey.ExportPkixText()))}"
        """);

        var verifierContainer = new ContainerBuilder()
                .WithImage(electricityVerifierImage)
                .WithResourceMapping(configFile, "/app/tmp/")
                .WithPortBinding(GrpcPort, true)
                .WithCommand("--serve")
                .WithEnvironment("Network__ConfigurationUri", "file:///app/tmp/" + Path.GetFileName(configFile))
                // .WithWaitStrategy(Wait.ForUnixContainer().UntilGrpcEndpointIsReady(GrpcPort, "/"))
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(GrpcPort, o => o.WithTimeout(TimeSpan.FromSeconds(10))))
                .Build();

        await verifierContainer.StartAsyncWithLogsAsync(outputHelper);
    }
}
