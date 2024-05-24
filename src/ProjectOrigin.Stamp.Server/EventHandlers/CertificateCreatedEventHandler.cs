using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.Extensions.Options;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Exceptions;
using ProjectOrigin.Stamp.Server.Helpers;
using ProjectOrigin.Stamp.Server.Options;
using ProjectOrigin.Stamp.Server.Services.REST.v1;

namespace ProjectOrigin.Stamp.Server.EventHandlers;

public record CertificateCreatedEvent
{
    public required Guid CertificateId { get; init; }
    public required string RegistryName { get; init; }
    public required IHDPublicKey WalletEndpointReferencePublicKey { get; init; }
    public required GranularCertificateType CertificateType { get; init; }
    public required uint Quantity { get; init; }
    public required long Start { get; init; }
    public required long End { get; init; }
    public required string GridArea { get; init; }
    public required Dictionary<string, string> ClearTextAttributes { get; init; }
    public required IEnumerable<HashedAttribute> HashedAttributes { get; init; }
}

public class CertificateCreatedEventHandler : IConsumer<CertificateCreatedEvent>
{
    private readonly IKeyGenerator _keyGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RegistryOptions _registryOptions;

    public CertificateCreatedEventHandler(IKeyGenerator keyGenerator,
        IUnitOfWork unitOfWork,
        IOptions<RegistryOptions> registryOptions)
    {
        _keyGenerator = keyGenerator;
        _unitOfWork = unitOfWork;
        _registryOptions = registryOptions.Value;
    }

    public async Task Consume(ConsumeContext<CertificateCreatedEvent> context)
    {
        var msg = context.Message;
        var endpointPosition = WalletEndpointPositionCalculator.CalculateWalletEndpointPosition(msg.Start);
        if (!endpointPosition.HasValue)
            throw new WalletException($"Cannot determine wallet endpoint position for certificate with id {msg.CertificateId}");

        var (ownerPublicKey, issuerKey) = _keyGenerator.GenerateKeyInfo(msg.WalletEndpointReferencePublicKey.Export().ToArray(), endpointPosition.Value, msg.GridArea);

        var commitment = new SecretCommitmentInfo(msg.Quantity);
        IssuedEvent issueEvent;

        if (msg.CertificateType == GranularCertificateType.Production)
        {
            var techCode = msg.ClearTextAttributes[Helpers.Registry.Attributes.TechCode];
            var fuelCode = msg.ClearTextAttributes[Helpers.Registry.Attributes.FuelCode];
            //TODO Set assetId correct
            issueEvent = Helpers.Registry.CreateIssuedEventForProduction(msg.RegistryName, msg.CertificateId, PeriodHelper.ToDateInterval(msg.Start, msg.End), msg.GridArea, "1234", techCode, fuelCode, commitment, ownerPublicKey);
        }
        else
        {
            //TODO Set assetId correct
            issueEvent = Helpers.Registry.CreateIssuedEventForConsumption(msg.RegistryName, msg.CertificateId, PeriodHelper.ToDateInterval(msg.Start, msg.End), msg.GridArea, "1234", commitment, ownerPublicKey);
        }

        using var channel = GrpcChannel.ForAddress(_registryOptions.GetRegistryUrl(msg.RegistryName));
        var client = new RegistryService.RegistryServiceClient(channel);

        var request = new SendTransactionsRequest();
        request.Transactions.Add(issueEvent.CreateTransaction(issuerKey));

        await client.SendTransactionsAsync(request);
    }
}
