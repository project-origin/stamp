using System;
using System.Threading.Tasks;
using Grpc.Core;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Stamp.Server.Exceptions;

namespace ProjectOrigin.Stamp.Server.EventHandlers;

public record CertificateSentToRegistryEvent
{
    public required string ShaId { get; init; }
    public required Guid CertificateId { get; init; }
    public required GranularCertificateType CertificateType { get; init; }
    public required string Registry { get; init; }
    public required uint Quantity { get; init; }
    public required byte[] RandomR { get; init; }
    public required uint WalletEndpointPosition { get; init; }
    public required byte[] WalletPublicKey { get; init; }
    public required string WalletUrl { get; init; }
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
                _logger.LogInformation("Transaction {id} with certificate {certificateId} completed.", message.ShaId, message.CertificateId);

                //await context.Publish<CertificateIssuedInRegistryEvent>(new CertificateIssuedInRegistryEvent
                //{
                //    CertificateId = message.CertificateId,
                //    MeteringPointType = message.MeteringPointType,
                //    Quantity = message.Quantity,
                //    RandomR = message.RandomR,
                //    WalletUrl = message.WalletUrl,
                //    WalletEndpointPosition = message.WalletEndpointPosition,
                //    WalletPublicKey = message.WalletPublicKey,
                //    Registry = message.Registry
                //});
                return;
            }

            if (status.Status == TransactionState.Failed)
            {
                _logger.LogWarning("Transaction {id} with certificate {certificateId} failed in registry.", message.ShaId, message.CertificateId);

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
            _logger.LogWarning("Registry communication error. Exception: {ex}", ex);
            throw new TransientException("Registry communication error");
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
