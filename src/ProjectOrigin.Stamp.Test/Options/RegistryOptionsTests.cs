using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectOrigin.Stamp.Server.Options;
using Xunit;

namespace ProjectOrigin.Stamp.Test.Options;

public class RegistryOptionsTests
{

    [Fact]
    public void ShouldTellSupportedRegistriesOnNotFoundRegistry()
    {
        // Arrange
        var registryOptions = new RegistryOptions
        {
            RegistryUrls = new Dictionary<string, string>
            {
                { "Narnia", "http://foo" },
                { "TestRegistry", "http://foo" },
                { "death-star", "http://foo" }
            }
        };

        // Act
        var sut = () => registryOptions.GetRegistryUrl("Narnia2");

        // Assert
        sut.Should().Throw<NotSupportedException>()
            .WithMessage("RegistryName Narnia2 not supported. Supported registries are: Narnia, TestRegistry, death-star");
    }

    [Fact]
    public void ShouldTellSupportedGridAreasOnNotFoundGridArea()
    {
        var registryOptions = new RegistryOptions
        {
            IssuerPrivateKeyPems = new Dictionary<string, byte[]>
            {
                { "Narnia", Encoding.UTF8.GetBytes("key") },
                { "TestArea", Encoding.UTF8.GetBytes("key") },
                { "death-star", Encoding.UTF8.GetBytes("key") }
            }
        };

        var sut = () => registryOptions.GetIssuerKey("Narnia2");

        sut.Should().Throw<NotSupportedException>()
            .WithMessage("Not supported GridArea Narnia2. Supported GridAreas are: Narnia, TestArea, death-star");
    }

    [Fact]
    public void ShouldCorrectlyConvertUnderscoresToHyphensInRegistryUrls()
    {

        Environment.SetEnvironmentVariable("RegistryUrls__kebab-case", "http://kebab-registry.com");
        Environment.SetEnvironmentVariable("RegistryUrls__camelCase", "http://camel-case-registry.com");

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var serviceProvider = new ServiceCollection()
            .Configure<RegistryOptions>(configuration)
            .BuildServiceProvider();

        var options = serviceProvider.GetService<IOptions<RegistryOptions>>();

        options!.Value.RegistryUrls.Should().ContainKey("kebab-case");
        options.Value.RegistryUrls[key: "kebab-case"].Should().Be("http://kebab-registry.com");
        options!.Value.RegistryUrls.Should().ContainKey("camelCase");
        options.Value.RegistryUrls[key: "camelCase"].Should().Be("http://camel-case-registry.com");
    }
}
