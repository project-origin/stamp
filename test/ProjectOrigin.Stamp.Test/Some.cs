using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.Stamp.Services.REST.v1;
using ProjectOrigin.Stamp.Test.Extensions;
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

    public static CertificateDto CertificateDto(string gridArea = "DK", uint quantity = 10, DateTimeOffset? start = null, DateTimeOffset? end = null, string? gsrn = null, Services.REST.v1.CertificateType type = Services.REST.v1.CertificateType.Consumption)
    {
        var startTime = start ?? DateTimeOffset.UtcNow;
        var endTime = end ?? DateTimeOffset.UtcNow.AddHours(1);
        return new CertificateDto
        {
            Id = Guid.NewGuid(),
            Start = startTime.RoundToLatestHourLong(),
            End = endTime.RoundToLatestHourLong(),
            GridArea = gridArea,
            Quantity = quantity,
            Type = type,
            ClearTextAttributes = new Dictionary<string, string>
            {
                { "fuelCode", "F01040100" },
                { "techCode", "T010000" }
            },
            HashedAttributes = new List<HashedAttribute>
            {
                new () { Key = "assetId", Value = gsrn ?? Gsrn() },
                new () { Key = "address", Value = "Some road 1234" }
            }
        };
    }
}
