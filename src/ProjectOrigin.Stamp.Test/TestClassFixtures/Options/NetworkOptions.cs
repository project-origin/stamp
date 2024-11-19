using ProjectOrigin.TestCommon;
using YamlDotNet.Serialization;

namespace ProjectOrigin.Stamp.Test.TestClassFixtures.Options;

public record NetworkOptions
{
    public IDictionary<string, RegistryInfo> Registries { get; init; } = new Dictionary<string, RegistryInfo>();
    public IDictionary<string, AreaInfo> Areas { get; init; } = new Dictionary<string, AreaInfo>();
    public IDictionary<string, IssuerInfo> Issuers { get; init; } = new Dictionary<string, IssuerInfo>();
}

public record RegistryInfo
{
    public required string Url { get; init; }
}

public class AreaInfo
{
    public required IList<KeyInfo> IssuerKeys { get; set; }
}

public record KeyInfo
{
    public required string PublicKey { get; init; }
}

public record IssuerInfo
{
    public required string StampUrl { get; init; }
}


public static class NetworkOptionsExtensions
{
    public static string ToTempYamlFileUri(this NetworkOptions networkOptions)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(networkOptions);
        var path = "file://" + TempFile.WriteAllText(yaml, ".yaml");
        return path;
    }

    public static string ToTempYamlFile(this NetworkOptions networkOptions)
    {
        var configFile = Path.GetTempFileName() + ".yaml";
        var serializer = new SerializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(networkOptions);
        File.WriteAllText(configFile, yaml);
        return configFile;
    }
}
