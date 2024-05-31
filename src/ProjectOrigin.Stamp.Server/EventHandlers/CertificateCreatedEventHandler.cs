using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.Extensions.Options;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Stamp.Server.Exceptions;
using ProjectOrigin.Stamp.Server.Helpers;
using ProjectOrigin.Stamp.Server.Models;
using ProjectOrigin.Stamp.Server.Options;
using GranularCertificateType = ProjectOrigin.Stamp.Server.Models.GranularCertificateType;

namespace ProjectOrigin.Stamp.Server.EventHandlers;

public record CertificateCreatedEvent
{
    public required Guid CertificateId { get; init; }
    public required string RegistryName { get; init; }
    public required byte[] WalletEndpointReferencePublicKey { get; init; }
    public required Guid RecipientId { get; init; }
    public required GranularCertificateType CertificateType { get; init; }
    public required uint Quantity { get; init; }
    public required long Start { get; init; }
    public required long End { get; init; }
    public required string GridArea { get; init; }
    public required Dictionary<string, string> ClearTextAttributes { get; init; }
    public required IEnumerable<CertificateHashedAttribute> HashedAttributes { get; init; }
}

public class CertificateCreatedEventHandler : IConsumer<CertificateCreatedEvent>
{
    private readonly IKeyGenerator _keyGenerator;
    private readonly RegistryOptions _registryOptions;

    public CertificateCreatedEventHandler(IKeyGenerator keyGenerator,
        IOptions<RegistryOptions> registryOptions)
    {
        _keyGenerator = keyGenerator;
        _registryOptions = registryOptions.Value;
    }

    public async Task Consume(ConsumeContext<CertificateCreatedEvent> context)
    {
        var message = context.Message;
        var endpointPosition = WalletEndpointPositionCalculator.CalculateWalletEndpointPosition(message.Start);
        if (!endpointPosition.HasValue)
            throw new WalletException($"Cannot determine wallet endpoint position for certificate with id {message.CertificateId}");

        var (ownerPublicKey, issuerKey) = _keyGenerator.GenerateKeyInfo(message.WalletEndpointReferencePublicKey, endpointPosition.Value, message.GridArea);

        var commitment = new SecretCommitmentInfo(message.Quantity);

        IssuedEvent issueEvent = Helpers.Registry.BuildIssuedEvent(message.RegistryName, message.CertificateId,
            PeriodHelper.ToDateInterval(message.Start, message.End), message.GridArea,
            commitment, ownerPublicKey, message.CertificateType.MapToRegistryModel(), message.ClearTextAttributes,
            message.HashedAttributes.ToList());

        using var channel = GrpcChannel.ForAddress(_registryOptions.GetRegistryUrl(message.RegistryName));
        var client = new RegistryService.RegistryServiceClient(channel);

        var request = new SendTransactionsRequest();
        var transaction = issueEvent.CreateTransaction(issuerKey);
        request.Transactions.Add(transaction);

        await client.SendTransactionsAsync(request);

        await context.Publish<CertificateSentToRegistryEvent>(new CertificateSentToRegistryEvent
        {
            ShaId = transaction.ToShaId(),
            CertificateId = message.CertificateId,
            RegistryName = message.RegistryName,
            RecipientId = message.RecipientId,
            WalletEndpointPosition = endpointPosition.Value,
            RandomR = commitment.BlindingValue.ToArray()
        });
    }
}
