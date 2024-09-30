using System;
using ProjectOrigin.Stamp.Models;

namespace ProjectOrigin.Stamp.Services.REST.v1;

public static class MappingExtensions
{
    public static GranularCertificateType MapToModel(this CertificateType certificateType) =>
        certificateType switch
        {
            CertificateType.Consumption => GranularCertificateType.Consumption,
            CertificateType.Production => GranularCertificateType.Production,
            _ => throw new ArgumentOutOfRangeException(nameof(certificateType), certificateType, null)
        };
}
