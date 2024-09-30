using System;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Stamp.Models;

public class Recipient
{
    public required Guid Id { get; init; }
    public required int WalletEndpointReferenceVersion { get; init; }
    public required string WalletEndpointReferenceEndpoint { get; init; }
    public required IHDPublicKey WalletEndpointReferencePublicKey { get; init; }
}
