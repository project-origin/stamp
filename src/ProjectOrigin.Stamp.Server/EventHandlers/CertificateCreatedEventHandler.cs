using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using ProjectOrigin.Stamp.Server.Services.REST.v1;

namespace ProjectOrigin.Stamp.Server.EventHandlers;

public record CertificateCreatedEvent
{
    public Guid CertificateId { get; init; }
    public string RegistryName { get; init; }
    public Guid RecipientId { get; init; }
    public CertificateType Type { get; init; }
    public uint Quantity { get; init; }
    public long Start { get; init; }
    public long End { get; init; }
    public string GridArea { get; init; }
    public Dictionary<string, string> ClearTextAttributes { get; init; }
    public IEnumerable<HashedAttribute> HashedAttributes { get; init; }
}

public class CertificateCreatedEventHandler : IConsumer<CertificateCreatedEvent>
{
    public Task Consume(ConsumeContext<CertificateCreatedEvent> context)
    {
        throw new NotImplementedException();
    }
}
