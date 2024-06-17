using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;

namespace ProjectOrigin.Stamp.Test;

public static class Some
{
    public static byte[] WalletPublicKey()
    {
        var privateKey = new Secp256k1Algorithm().GenerateNewPrivateKey();

        return privateKey.Neuter().Export().ToArray();
    }
}
