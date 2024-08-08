using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
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
    public void ShouldAccessRegistryUrlDirectlyFromDictionary()
    {
        var expectedUrl = "http://energy-origin-registry:80";
        var registryOptions = new RegistryOptions
        {
            RegistryUrls = new Dictionary<string, string>
            {
                { "energy-origin", expectedUrl },
                { "other-registry", "http://other-registry:80" }
            }
        };

        var url = registryOptions.RegistryUrls["energy-origin"];

        url.Should().Be(expectedUrl);
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
