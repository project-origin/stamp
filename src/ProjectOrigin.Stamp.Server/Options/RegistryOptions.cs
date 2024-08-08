using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ProjectOrigin.Stamp.Server.Options;

public class RegistryOptions
{
    [Required]
    public Dictionary<string, string> RegistryUrls { get; set; } = new Dictionary<string, string>();

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
        // First, try to get the value directly (case-sensitive)
        if (RegistryUrls.TryGetValue(name, out var url))
        {
            return url;
        }

        // If not found, try case-insensitive search
        var key = RegistryUrls.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (key != null)
        {
            return RegistryUrls[key];
        }

        // If still not found, throw the exception
        string registries = string.Join(", ", RegistryUrls.Keys);
        throw new NotSupportedException($"RegistryName {name} not supported. Supported registries are: " + registries);
    }

    private static IPrivateKey ToPrivateKey(byte[] key)
        => new Ed25519Algorithm().ImportPrivateKeyText(Encoding.UTF8.GetString(key));
}
