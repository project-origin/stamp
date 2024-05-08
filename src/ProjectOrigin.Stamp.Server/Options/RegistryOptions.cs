using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProjectOrigin.Stamp.Server.Options;

public class RegistryOptions
{
    public const string Registry = nameof(Registry);

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

        throw new NotSupportedException($"Not supported GridArea {gridArea}");
    }

    public string GetRegistryUrl(string name)
    {
        if (RegistryUrls.TryGetValue(name, out var url))
        {
            return url;
        }

        throw new NotSupportedException($"Registry {name} not supported");
    }

    private static IPrivateKey ToPrivateKey(byte[] key)
        => new Ed25519Algorithm().ImportPrivateKeyText(Encoding.UTF8.GetString(key));
}

public static class OptionsExtensions
{
    public static void AddRegistryOptions(this IServiceCollection services) =>
        services.AddOptions<RegistryOptions>()
            .BindConfiguration(RegistryOptions.Registry)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
