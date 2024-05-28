using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Stamp.Server.Database;

namespace ProjectOrigin.Stamp.Server.EventHandlers;

public record CertificateFailedInRegistryEvent
{
    public required string RejectReason { get; init; }
    public required Guid CertificateId { get; init; }
    public required string RegistryName { get; init; }
}

public class CertificateFailedInRegistryEventHandler : IConsumer<CertificateFailedInRegistryEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CertificateFailedInRegistryEventHandler> _logger;

    public CertificateFailedInRegistryEventHandler(IUnitOfWork unitOfWork, ILogger<CertificateFailedInRegistryEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CertificateFailedInRegistryEvent> context)
    {
        var message = context.Message;

        var certificate = await _unitOfWork.CertificateRepository.Get(message.RegistryName, message.CertificateId);

        if (certificate == null)
        {
            _logger.LogWarning("Certificate with certificateId {certificateId} and registry name {registryName} not found.", message.CertificateId, message.RegistryName);
            return;
        }

        if (certificate.IsRejected)
        {
            _logger.LogWarning("Certificate with certificateId {certificateId} and registry name {registryName} already rejected.", message.CertificateId, message.RegistryName);
            return;
        }

        certificate.Reject(message.RejectReason);

        await _unitOfWork.CertificateRepository.SetState(message.CertificateId, message.RegistryName, certificate.IssuedState, message.RejectReason);
        _unitOfWork.Commit();
        _logger.LogInformation("Certificate with certificateId {certificateId} and registry name {registryName} rejected", message.CertificateId, message.RegistryName);
    }
}
