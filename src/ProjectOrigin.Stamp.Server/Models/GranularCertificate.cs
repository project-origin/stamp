using System;
using System.Collections.Generic;
using ProjectOrigin.Stamp.Server.Exceptions;

namespace ProjectOrigin.Stamp.Server.Models;

public enum GranularCertificateType
{
    Consumption = 1,
    Production = 2
}

public enum IssuedState
{
    Creating = 1,
    Issued = 2,
    Rejected = 3
}

public record CertificateClearTextAttribute
{
    public required string Key { get; init; }
    public required string Value { get; init; }
}

public record CertificateHashedAttribute()
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public required byte[] Salt { get; init; }
}

public record GranularCertificate
{
    public required Guid Id { get; init; }
    public required string RegistryName { get; init; }
    public required GranularCertificateType CertificateType { get; init; }
    public required uint Quantity { get; init; }
    public required long StartDate { get; init; }
    public required long EndDate { get; init; }
    public required string GridArea { get; init; }
    public required Dictionary<string, string> ClearTextAttributes { get; init; } = new();
    public required List<CertificateHashedAttribute> HashedAttributes { get; init; } = new();

    public IssuedState IssuedState { get; private set; } = IssuedState.Creating;
    public string? RejectionReason { get; private set; }
    public bool IsRejected => IssuedState == IssuedState.Rejected;
    public bool IsIssued => IssuedState == IssuedState.Issued;

    public void Reject(string reason)
    {
        if (IssuedState != IssuedState.Creating)
            throw new CertificateDomainException(Id, $"Cannot reject when certificate is already {IssuedState.ToString()!.ToLower()}");

        IssuedState = IssuedState.Rejected;
        RejectionReason = reason;
    }

    public void Issue()
    {
        if (IssuedState != IssuedState.Creating)
            throw new CertificateDomainException(Id, $"Cannot issue when certificate is already {IssuedState.ToString()!.ToLower()}");

        IssuedState = IssuedState.Issued;
    }
}
