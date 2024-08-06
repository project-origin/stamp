using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectOrigin.Stamp.Server;
using ProjectOrigin.Stamp.Server.Options;
using Xunit;

namespace ProjectOrigin.Stamp.Test.Options;

public class RegistryOptionsTests
{
    [Theory]
    [InlineData("kebab-case", "https://kebab-case.com:80")]
    [InlineData("camelCase", "https://camel-case-registry.com:443")]
    [InlineData("under_score", "http://under-score-registry:80")]
    [InlineData("BIGLETTERS", "http://big-letters-registry.com")]
    [InlineData("smallletters", "http://small-letters-registry.com")]
    [InlineData("Mixed-Case_with-Hyphens", "http://mixed-case-registry.com")]
    [InlineData("123numeric", "http://numeric-registry.com")]
    [InlineData("special@#$characters", "http://special-characters-registry.com")]
    [InlineData("A1T_Godt-fr@-HaVeT", "http://alt-godt-fra-havet-registry.com")]
    public void ShouldCorrectlyMapRegistryUrlFromHelmChart(string registryName, string expectedUrl)
    {
        try
        {
            Environment.SetEnvironmentVariable($"RegistryUrls__{registryName}", expectedUrl);

            var services = new ServiceCollection();

            var startup = new Startup(new ConfigurationBuilder().Build());
            var registryUrls = startup.LoadRegistryUrlsWithHyphens();

            services.Configure<RegistryOptions>(options =>
            {
                options.RegistryUrls = registryUrls;
            });

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IOptions<RegistryOptions>>()?.Value;

            options.Should().NotBeNull();
            options!.RegistryUrls.Should().ContainKey(registryName);
            options.RegistryUrls[registryName].Should().Be(expectedUrl);
            options.GetRegistryUrl(registryName).Should().Be(expectedUrl);
        }
        finally
        {
            Environment.SetEnvironmentVariable($"RegistryUrls__{registryName}", null);
        }
    }

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
}
