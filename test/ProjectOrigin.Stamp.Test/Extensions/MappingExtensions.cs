
namespace ProjectOrigin.Stamp.Test.Extensions;

public static class MappingExtensions
{
    public static CertificateType MapToWalletModel(this Services.REST.v1.CertificateType certificateType) =>
        certificateType switch
        {
            Services.REST.v1.CertificateType.Consumption => CertificateType.Consumption,
            Services.REST.v1.CertificateType.Production => CertificateType.Production,
            _ => throw new ArgumentOutOfRangeException(nameof(certificateType), certificateType, null)
        };
}
