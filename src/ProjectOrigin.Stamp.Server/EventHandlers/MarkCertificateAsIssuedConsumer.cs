using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Extensions;
using ProjectOrigin.Stamp.Server.Models;
using Microsoft.Extensions.Options;
using ProjectOrigin.Stamp.Server.Metrics;
using ProjectOrigin.Stamp.Server.Options;

namespace ProjectOrigin.Stamp.Server.EventHandlers;

public record CertificateIssuedInRegistryEvent
{
    public required Guid CertificateId { get; init; }
    public required string Registry { get; init; }
    public required Guid RecipientId { get; init; }
    public required uint WalletEndpointPosition { get; init; }
    public required byte[] RandomR { get; init; }
}

public class MarkCertificateAsIssuedConsumer : IConsumer<CertificateIssuedInRegistryEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkCertificateAsIssuedConsumer> _logger;
    private readonly IStampMetrics _stampMetrics;


    public MarkCertificateAsIssuedConsumer(IUnitOfWork unitOfWork,
        ILogger<MarkCertificateAsIssuedConsumer> logger,
        IStampMetrics stampMetrics)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _stampMetrics = stampMetrics;
    }

    public async Task Consume(ConsumeContext<CertificateIssuedInRegistryEvent> context)
    {
        var message = context.Message;

        var certificate = await _unitOfWork.CertificateRepository.Get(message.Registry, message.CertificateId);

        if (certificate == null)
        {
            _logger.LogWarning("Certificate with registry {Registry} and certificateId {CertificateId} not found.", message.Registry, message.CertificateId);
            return;
        }

        if (!certificate.IsIssued)
            certificate.Issue();

        await _unitOfWork.CertificateRepository.SetState(message.CertificateId, message.Registry, certificate.IssuedState);

        _stampMetrics.AddCertificatesIssued(1);
        _stampMetrics.UpdateGauges();

        var payloadObj = new CertificateMarkedAsIssuedEvent
        {
            CertificateId = message.CertificateId,
            Quantity = certificate.Quantity,
            RandomR = message.RandomR,
            RecipientId = message.RecipientId,
            RegistryName = message.Registry,
            WalletEndpointPosition = message.WalletEndpointPosition,
            HashedAttributes = certificate.HashedAttributes
        };
        await _unitOfWork.OutboxMessageRepository.Create(new OutboxMessage
        {
            Created = DateTimeOffset.UtcNow.ToUtcTime(),
            Id = Guid.NewGuid(),
            MessageType = typeof(CertificateMarkedAsIssuedEvent).ToString(),
            JsonPayload = JsonSerializer.Serialize(payloadObj)
        });
        _unitOfWork.Commit();

        _logger.LogInformation("Certificate with registry {Registry} and certificateId {CertificateId} issued.", message.Registry, message.CertificateId);
    }
}

public class MarkCertificateAsIssuedConsumerDefinition : ConsumerDefinition<MarkCertificateAsIssuedConsumer>
{
    private readonly RetryOptions _retryOptions;

    public MarkCertificateAsIssuedConsumerDefinition(IOptions<RetryOptions> options)
    {
        _retryOptions = options.Value;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<MarkCertificateAsIssuedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(_retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
