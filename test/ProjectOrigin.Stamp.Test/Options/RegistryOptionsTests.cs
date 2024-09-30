using System.Text;
using FluentAssertions;
using ProjectOrigin.Stamp.Options;
using Xunit;

namespace ProjectOrigin.Stamp.Test.OptionsTests;

public class RegistryOptionsTests
{

    [Fact]
    public void ShouldTellSupportedRegistriesOnNotFoundRegistry()
    {
        var registryOptions = new RegistryOptions
        {
            Registries = new List<Options.Registry>
            {
                new()  { Name = "Narnia", Address = "http://foo"},
                new()  {Name = "TestRegistry", Address = "http://foo" },
                new() {Name = "death-star", Address = "http://foo" }
            }
        };

        var sut = () => registryOptions.GetRegistryUrl("Narnia2");

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
