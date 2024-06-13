using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Stamp.Server.Exceptions;
using ProjectOrigin.Stamp.Server.Options;
using RegistryOptions = ProjectOrigin.Stamp.Server.Options.RegistryOptions;

namespace ProjectOrigin.Stamp.Server.EventHandlers;

public record CertificateSentToRegistryEvent
{
    public required string ShaId { get; init; }
    public required Guid CertificateId { get; init; }
    public required string RegistryName { get; init; }
    public required Guid RecipientId { get; init; }
    public required uint WalletEndpointPosition { get; init; }
    public required byte[] RandomR { get; init; }
}   

public class CertificateSentToRegistryEventHandler : IConsumer<CertificateSentToRegistryEvent>
{
    private readonly ILogger<CertificateSentToRegistryEventHandler> _logger;
    private readonly RegistryOptions _registryOptions;

    public CertificateSentToRegistryEventHandler(ILogger<CertificateSentToRegistryEventHandler> logger, IOptions<RegistryOptions> registryOptions)
    {
        _logger = logger;
        _registryOptions = registryOptions.Value;
    }

    public async Task Consume(ConsumeContext<CertificateSentToRegistryEvent> context)
    {
        var message = context.Message;
        var statusRequest = new GetTransactionStatusRequest { Id = message.ShaId };

        try
        {
            using var channel = GrpcChannel.ForAddress(_registryOptions.GetRegistryUrl(message.RegistryName));
            var client = new RegistryService.RegistryServiceClient(channel);
            var status = await client.GetTransactionStatusAsync(statusRequest);

            if (status.Status == TransactionState.Committed)
            {
                _logger.LogInformation("Registry transaction {id} with certificateId {certificateId} completed.", message.ShaId, message.CertificateId);

                await context.Publish<CertificateIssuedInRegistryEvent>(new CertificateIssuedInRegistryEvent
                {
                    CertificateId = message.CertificateId,
                    RecipientId = message.RecipientId,
                    Registry = message.RegistryName,
                    WalletEndpointPosition = message.WalletEndpointPosition,
                    RandomR = message.RandomR
                });
                return;
            }

            if (status.Status == TransactionState.Failed)
            {
                _logger.LogWarning("Registry transaction {id} with certificateId {certificateId} failed in registry.", message.ShaId, message.CertificateId);

                await context.Publish<CertificateFailedInRegistryEvent>(new CertificateFailedInRegistryEvent
                {
                    RegistryName = message.RegistryName,
                    CertificateId = message.CertificateId,
                    RejectReason = "Rejected by the registry"
                });
                return;
            }

            string infoMessage = $"Transaction {message.ShaId} is still processing on registry for certificateId: {message.CertificateId}.";
            _logger.LogInformation(infoMessage);
            throw new RegistryTransactionStillProcessingException(infoMessage);
        }
        catch (RpcException ex)
        {
            _logger.LogWarning("RegistryName {registryName} communication error. Exception: {ex}", message.RegistryName, ex);
            throw new TransientException($"RegistryName {message.RegistryName} communication error");
        }
    }
}

[Serializable]
public class RegistryTransactionStillProcessingException : Exception
{
    public RegistryTransactionStillProcessingException(string message) : base(message)
    {
    }
}

public class CertificateSentToRegistryEventHandlerDefinition : ConsumerDefinition<CertificateSentToRegistryEventHandler>
{
    private readonly RetryOptions _retryOptions;

    public CertificateSentToRegistryEventHandlerDefinition(IOptions<RetryOptions> options)
    {
        _retryOptions = options.Value;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CertificateSentToRegistryEventHandler> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Interval(_retryOptions.RegistryTransactionStillProcessingRetryCount, TimeSpan.FromSeconds(1))
            .Handle(typeof(RegistryTransactionStillProcessingException)));

        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(_retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3))
            .Ignore(typeof(RegistryTransactionStillProcessingException)));
    }
}

