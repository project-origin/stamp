using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Options;

namespace ProjectOrigin.Stamp.Server.EventHandlers;

public record CertificateFailedInRegistryEvent
{
    public required string RejectReason { get; init; }
    public required Guid CertificateId { get; init; }
    public required string RegistryName { get; init; }
}

public class RejectCertificateConsumer : IConsumer<CertificateFailedInRegistryEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectCertificateConsumer> _logger;

    public RejectCertificateConsumer(IUnitOfWork unitOfWork, ILogger<RejectCertificateConsumer> logger)
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

public class RejectCertificateConsumerDefinition : ConsumerDefinition<RejectCertificateConsumer>
{
    private readonly RetryOptions _retryOptions;

    public RejectCertificateConsumerDefinition(IOptions<RetryOptions> options)
    {
        _retryOptions = options.Value;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<RejectCertificateConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(_retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
