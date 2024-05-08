using MassTransit;
using ProjectOrigin.Stamp.Server.Services.REST.v1;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Exceptions;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Stamp.Server.Helpers;

namespace ProjectOrigin.Stamp.Server.CommandHandlers;

public record IssueCertificateCommand
{
    public required Guid RecipientId { get; init; }
    public required string RegistryName { get; init; }
    public required Guid CertificateId { get; init; }
    public required CertificateType Type { get; init; }
    public required uint Quantity { get; init; }
    public required long Start { get; init; }
    public required long End { get; init; }
    public required string GridArea { get; init; }
    public required Dictionary<string, string> ClearTextAttributes { get; init; }
    public required IEnumerable<HashedAttribute> HashedAttributes { get; init; }
}

public class IssueCertificateCommandHandler : IConsumer<IssueCertificateCommand>
{
    private readonly RegistryService.RegistryServiceClient _client;
    private readonly IKeyGenerator _keyGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IssueCertificateCommandHandler> _logger;

    public IssueCertificateCommandHandler(RegistryService.RegistryServiceClient client, IKeyGenerator keyGenerator, IUnitOfWork unitOfWork, ILogger<IssueCertificateCommandHandler> logger)
    {
        _client = client;
        _keyGenerator = keyGenerator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IssueCertificateCommand> context)
    {
        _logger.LogInformation("Issuing to Registry for certificate id {certificateId}.", context.Message.CertificateId);
        var msg = context.Message;
        //Get from db and if not present: save to db (if GSRN is available)

        //Send to registry
        var recipient = await _unitOfWork.RecipientRepository.Get(msg.RecipientId);

        var endpointPosition = WalletEndpointPositionCalculator.CalculateWalletEndpointPosition(msg.Start);
        if (!endpointPosition.HasValue)
            throw new WalletException($"Cannot determine wallet position for certificate with id {msg.CertificateId}");

        var (ownerPublicKey, issuerKey) = _keyGenerator.GenerateKeyInfo(recipient!.WalletEndpointReferencePublicKey.Export().ToArray(), endpointPosition.Value, msg.GridArea);

        var commitment = new SecretCommitmentInfo(msg.Quantity);
        IssuedEvent issueEvent;

        if (msg.Type == CertificateType.Production)
        {
            var techCode = msg.ClearTextAttributes[Helpers.Registry.Attributes.TechCode];
            var fuelCode = msg.ClearTextAttributes[Helpers.Registry.Attributes.FuelCode];
            issueEvent = Helpers.Registry.CreateIssuedEventForProduction(msg.RegistryName, msg.CertificateId, PeriodHelper.ToDateInterval(msg.Start, msg.End), msg.GridArea, recipient.GSRN, techCode, fuelCode, commitment, ownerPublicKey);
        }
        else
        {
            issueEvent = Helpers.Registry.CreateIssuedEventForConsumption(msg.RegistryName, msg.CertificateId, new DateInterval { Start = msg.Start, End = msg.End }, msg.GridArea, recipient.GSRN, commitment, ownerPublicKey);
        }

        //Skal assetId med i dto?
        var request = new SendTransactionsRequest();
        request.Transactions.Add(context.Arguments.Transaction);

        await _client.SendTransactionsAsync(request);

    }
}
