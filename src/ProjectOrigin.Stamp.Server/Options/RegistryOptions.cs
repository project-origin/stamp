using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Stamp.Server.Options;

public class RegistryOptions
{
    private const string RegistryUrlPrefix = "RegistryUrls__";

    [Required]
    public Dictionary<string, string> RegistryUrls { get; set; } = new();

    [Required]
    public Dictionary<string, byte[]> IssuerPrivateKeyPems { get; set; } = new();

    public RegistryOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var registryUrlsJson = Environment.GetEnvironmentVariable("RegistryUrls");
        RegistryUrls = (!string.IsNullOrEmpty(registryUrlsJson)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(registryUrlsJson, options)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))!;
    }

    public bool TryGetIssuerKey(string gridArea, out IPrivateKey? issuerKey)
    {
        try
        {
            issuerKey = GetIssuerKey(gridArea);
            return true;
        }
        catch (Exception)
        {
            issuerKey = default;
            return false;
        }
    }

    public IPrivateKey GetIssuerKey(string gridArea)
    {
        if (IssuerPrivateKeyPems.TryGetValue(gridArea, out var issuerPrivateKeyPem))
        {
            return ToPrivateKey(issuerPrivateKeyPem);
        }

        string gridAreas = string.Join(", ", IssuerPrivateKeyPems.Keys);
        throw new NotSupportedException($"Not supported GridArea {gridArea}. Supported GridAreas are: " + gridAreas);
    }

    public string GetRegistryUrl(string name)
    {
        if (RegistryUrls.TryGetValue(name, out var url))
        {
            return url;
        }

        string registries = string.Join(", ", RegistryUrls.Keys);
        throw new NotSupportedException($"RegistryName {name} not supported. Supported registries are: " + registries);
    }

    private static IPrivateKey ToPrivateKey(byte[] key)
        => new Ed25519Algorithm().ImportPrivateKeyText(Encoding.UTF8.GetString(key));
}
