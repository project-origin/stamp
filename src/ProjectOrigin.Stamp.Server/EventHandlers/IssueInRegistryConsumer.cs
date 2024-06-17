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
using ProjectOrigin.Stamp.Server.ValueObjects;
using GranularCertificateType = ProjectOrigin.Stamp.Server.Models.GranularCertificateType;

namespace ProjectOrigin.Stamp.Server.EventHandlers;

public class CertificateStoredEvent
{
    public required Guid CertificateId { get; init; }
    public required string RegistryName { get; init; }
    public required byte[] WalletEndpointReferencePublicKey { get; init; }
    public required Guid RecipientId { get; init; }
    public required GranularCertificateType CertificateType { get; init; }
    public required uint Quantity { get; init; }
    public required Period Period { get; init; }
    public required string GridArea { get; init; }
    public required Dictionary<string, string> ClearTextAttributes { get; init; }
    public required IEnumerable<CertificateHashedAttribute> HashedAttributes { get; init; }
}

public class IssueInRegistryConsumer : IConsumer<CertificateStoredEvent>
{
    private readonly IKeyGenerator _keyGenerator;
    private readonly RegistryOptions _registryOptions;

    public IssueInRegistryConsumer(IKeyGenerator keyGenerator,
        IOptions<RegistryOptions> registryOptions)
    {
        _keyGenerator = keyGenerator;
        _registryOptions = registryOptions.Value;
    }

    public async Task Consume(ConsumeContext<CertificateStoredEvent> context)
    {
        var message = context.Message;
        var endpointPosition = WalletEndpointPositionCalculator.CalculateWalletEndpointPosition(message.Period.DateFrom);
        if (!endpointPosition.HasValue)
            throw new WalletException($"Cannot determine wallet endpoint position for certificate with id {message.CertificateId}");

        var (ownerPublicKey, issuerKey) = _keyGenerator.GenerateKeyInfo(message.WalletEndpointReferencePublicKey, endpointPosition.Value, message.GridArea);

        var commitment = new SecretCommitmentInfo(message.Quantity);

        IssuedEvent issueEvent = Helpers.Registry.BuildIssuedEvent(message.RegistryName, message.CertificateId,
            message.Period.ToDateInterval(), message.GridArea,
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

public class IssueInRegistryConsumerDefinition : ConsumerDefinition<IssueInRegistryConsumer>
{
    private readonly RetryOptions _retryOptions;

    public IssueInRegistryConsumerDefinition(IOptions<RetryOptions> options)
    {
        _retryOptions = options.Value;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<IssueInRegistryConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(_retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
