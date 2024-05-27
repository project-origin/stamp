using System;
using System.Collections.Generic;
using System.Linq;
using ProjectOrigin.Stamp.Server.Models;

namespace ProjectOrigin.Stamp.Server.Services.REST.v1;

public static class MappingExtensions
{
    public static GranularCertificateType MapToModel(this CertificateType certificateType) =>
        certificateType switch
        {
            CertificateType.Consumption => GranularCertificateType.Consumption,
            CertificateType.Production => GranularCertificateType.Production,
            _ => throw new ArgumentOutOfRangeException(nameof(certificateType), certificateType, null)
        };

    public static IEnumerable<CertificateHashedAttribute> MapToModel(this IEnumerable<HashedAttribute> hashedAttributes) =>
        hashedAttributes.Select(ha => new CertificateHashedAttribute
        {
            Key = ha.Key,
            Value = ha.Value,
            Salt = ha.Salt
        });
}
