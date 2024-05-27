using System;
using System.Threading.Tasks;
using Grpc.Core;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Stamp.Server.Exceptions;

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
    private readonly RegistryService.RegistryServiceClient _client;
    private readonly ILogger<CertificateSentToRegistryEventHandler> _logger;

    public CertificateSentToRegistryEventHandler(RegistryService.RegistryServiceClient client, ILogger<CertificateSentToRegistryEventHandler> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CertificateSentToRegistryEvent> context)
    {
        var message = context.Message;
        var statusRequest = new GetTransactionStatusRequest { Id = message.ShaId };

        try
        {
            var status = await _client.GetTransactionStatusAsync(statusRequest);

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

                //await context.Publish<CertificateFailedInRegistryEvent>(new CertificateFailedInRegistryEvent
                //{
                //    MeteringPointType = message.MeteringPointType,
                //    CertificateId = message.CertificateId,
                //    RejectReason = "Rejected by the registry"
                //});
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
