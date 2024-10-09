using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Stamp.Server.Options;

public class Registry
{
    public required string Name { get; set; }
    public required string Address { get; set; }
}

public class RegistryOptions
{
    [Required]
    public IList<Registry> Registries { get; set; } = new List<Registry>();

    [Required]
    public Dictionary<string, byte[]> IssuerPrivateKeyPems { get; set; } = new Dictionary<string, byte[]>();

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
        var foundRegistry = Registries.SingleOrDefault(registry => registry.Name == name);
        if (foundRegistry is not null)
        {
            return foundRegistry.Address;
        }

        string registries = string.Join(", ", Registries.Select(registry => registry.Name));
        throw new NotSupportedException($"RegistryName {name} not supported. Supported registries are: " + registries);
    }

    private static IPrivateKey ToPrivateKey(byte[] key)
        => new Ed25519Algorithm().ImportPrivateKeyText(Encoding.UTF8.GetString(key));
}
