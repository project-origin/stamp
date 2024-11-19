using System;
using System.Collections.Generic;

namespace ProjectOrigin.Stamp.Server.Models;

public record WithdrawnCertificate
{
    public required int Id { get; set; }
    public required Guid CertificateId { get; init; }
    public required string RegistryName { get; init; }
    public required GranularCertificateType CertificateType { get; init; }
    public required uint Quantity { get; init; }
    public required long StartDate { get; init; }
    public required long EndDate { get; init; }
    public required string GridArea { get; init; }
    public required string MeteringPointId { get; init; }
    public required Dictionary<string, string> ClearTextAttributes { get; init; } = new();
    public required List<CertificateHashedAttribute> HashedAttributes { get; init; } = new();
    public required IssuedState IssuedState { get; init; }
    public string? RejectionReason { get; init; }
    public required DateTimeOffset WithdrawnDate { get; init; }
}
