using System;

namespace ProjectOrigin.Stamp.Models;

public record WithdrawnCertificate
{
    public required int Id { get; set; }
    public required Guid CertificateId { get; init; }
    public required string RegistryName { get; init; }
    public required DateTimeOffset WithdrawnDate { get; init; }
}
