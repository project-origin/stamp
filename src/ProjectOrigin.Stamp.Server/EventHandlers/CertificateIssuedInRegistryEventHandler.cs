using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Models;

namespace ProjectOrigin.Stamp.Server.EventHandlers;

public record CertificateIssuedInRegistryEvent
{
    public required Guid CertificateId { get; init; }
    public required GranularCertificateType CertificateType { get; init; }
    public required string Registry { get; init; }
    public required uint Quantity { get; init; }
    public required byte[] RandomR { get; init; }
    public required uint WalletEndpointPosition { get; init; }
    public required byte[] WalletPublicKey { get; init; }
    public required string WalletUrl { get; init; }
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
            _logger.LogWarning("Certificate with registry {message.Registry} and certificateId {message.CertificateId} not found.", message.Registry, message.CertificateId);
            return;
        }

        if (!certificate.IsIssued)
            certificate.Issue();

        await context.Publish<CertificateMarkedAsIssuedEvent>(new CertificateMarkedAsIssuedEvent
        {
            CertificateId = message.CertificateId,
            Registry = message.Registry,
            Quantity = message.Quantity,
            RandomR = message.RandomR,
            WalletEndpointPosition = message.WalletEndpointPosition,
            WalletPublicKey = message.WalletPublicKey,
            WalletUrl = message.WalletUrl
        });
        _unitOfWork.Commit();

        _logger.LogInformation("Certificate with registry {message.Registry} and certificateId {message.CertificateId} issued.", message.Registry, message.CertificateId);
    }
}
