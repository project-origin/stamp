using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Extensions;
using ProjectOrigin.Stamp.Server.Models;

namespace ProjectOrigin.Stamp.Server.EventHandlers;

public record CertificateIssuedInRegistryEvent
{
    public required Guid CertificateId { get; init; }
    public required string Registry { get; init; }
    public required Guid RecipientId { get; init; }
    public required uint WalletEndpointPosition { get; init; }
    public required byte[] RandomR { get; init; }
}

public class CertificateIssuedInRegistryEventHandler : IConsumer<CertificateIssuedInRegistryEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CertificateIssuedInRegistryEventHandler> _logger;

    public CertificateIssuedInRegistryEventHandler(IUnitOfWork unitOfWork,
        ILogger<CertificateIssuedInRegistryEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CertificateIssuedInRegistryEvent> context)
    {
        var message = context.Message;

        var certificate = await _unitOfWork.CertificateRepository.Get(message.Registry, message.CertificateId);

        if (certificate == null)
        {
            _logger.LogWarning("Certificate with registry {message.RegistryName} and certificateId {message.CertificateId} not found.", message.Registry, message.CertificateId);
            return;
        }

        if (!certificate.IsIssued)
            certificate.Issue();

        await _unitOfWork.CertificateRepository.SetState(message.CertificateId, message.Registry, certificate.IssuedState);

        var payloadObj = new CertificateMarkedAsIssuedEvent
        {
            CertificateId = message.CertificateId,
            Quantity = certificate.Quantity,
            RandomR = message.RandomR,
            RecipientId = message.RecipientId,
            RegistryName = message.Registry,
            WalletEndpointPosition = message.WalletEndpointPosition
        };
        await _unitOfWork.OutboxMessageRepository.Create(new OutboxMessage
        {
            Created = DateTimeOffset.UtcNow.ToUtcTime(),
            Id = Guid.NewGuid(),
            MessageType = typeof(CertificateMarkedAsIssuedEvent).ToString(),
            JsonPayload = JsonSerializer.Serialize(payloadObj)
        });
        _unitOfWork.Commit();

        _logger.LogInformation("Certificate with registry {message.RegistryName} and certificateId {message.CertificateId} issued.", message.Registry, message.CertificateId);
    }
}
