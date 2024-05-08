using Microsoft.Extensions.Options;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.Stamp.Server.Options;

namespace ProjectOrigin.Stamp.Server.Helpers;

public interface IKeyGenerator
{
    (IPublicKey, IPrivateKey) GenerateKeyInfo(byte[] walletPublicKey, uint walletDepositEndpointPosition, string gridArea);
}

public class KeyGenerator : IKeyGenerator
{
    private readonly RegistryOptions registryOptions;

    public KeyGenerator(IOptions<RegistryOptions> registryOptions)
    {
        this.registryOptions = registryOptions.Value;
    }

    public (IPublicKey, IPrivateKey) GenerateKeyInfo(byte[] walletPublicKey, uint walletDepositEndpointPosition, string gridArea)
    {
        var hdPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(walletPublicKey);
        var ownerPublicKey = hdPublicKey.Derive((int)walletDepositEndpointPosition).GetPublicKey();
        var issuerKey = registryOptions.GetIssuerKey(gridArea);

        return (ownerPublicKey, issuerKey);
    }
}
