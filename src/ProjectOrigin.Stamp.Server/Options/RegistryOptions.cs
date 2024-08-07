using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Stamp.Server.Options;

public class RegistryOptions
{
    [Required]
    public Dictionary<string, string> RegistryUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [Required]
    public Dictionary<string, byte[]> IssuerPrivateKeyPems { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public RegistryOptions()
    {
        PopulateFromEnvironment();
    }

    private void PopulateFromEnvironment()
    {
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            string key = entry.Key.ToString();
            string value = entry.Value.ToString();

            if (key.StartsWith("RegistryUrls__", StringComparison.OrdinalIgnoreCase))
            {
                string registryName = key.Substring("RegistryUrls__".Length);
                RegistryUrls[registryName] = value;
            }
            else if (key.StartsWith("IssuerPrivateKeyPems__", StringComparison.OrdinalIgnoreCase))
            {
                string gridArea = key.Substring("IssuerPrivateKeyPems__".Length);
                IssuerPrivateKeyPems[gridArea] = System.Text.Encoding.UTF8.GetBytes(value);
            }
        }
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
