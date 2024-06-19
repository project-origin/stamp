using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using System.Text;

namespace ProjectOrigin.Stamp.Test;

public static class Some
{
    public static byte[] WalletPublicKey()
    {
        var privateKey = new Secp256k1Algorithm().GenerateNewPrivateKey();

        return privateKey.Neuter().Export().ToArray();
    }

    public static string Gsrn()
    {
        var rand = new Random();
        var sb = new StringBuilder();
        sb.Append("57");
        for (var i = 0; i < 16; i++)
        {
            sb.Append(rand.Next(0, 9));
        }

        return sb.ToString();
    }
}
