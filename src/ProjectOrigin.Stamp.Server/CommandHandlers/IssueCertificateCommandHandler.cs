using MassTransit;
using ProjectOrigin.Stamp.Server.Services.REST.v1;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Exceptions;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Stamp.Server.Helpers;
using Microsoft.Extensions.Options;
using ProjectOrigin.Stamp.Server.Models;
using ProjectOrigin.Stamp.Server.Options;

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
    private readonly IKeyGenerator _keyGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IssueCertificateCommandHandler> _logger;
    private readonly RegistryOptions _registryOptions;

    public IssueCertificateCommandHandler(IKeyGenerator keyGenerator, IUnitOfWork unitOfWork, IOptions<RegistryOptions> registryOptions, ILogger<IssueCertificateCommandHandler> logger)
    {
        _keyGenerator = keyGenerator;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _registryOptions = registryOptions.Value;
    }

    public async Task Consume(ConsumeContext<IssueCertificateCommand> context)
    {
        _logger.LogInformation("Issuing to Registry for certificate id {certificateId}.", context.Message.CertificateId);
        var msg = context.Message;
        //Get from db and if not present: save to db (if GSRN is available)
        //Skal assetId med i dto?

        var cert = await _unitOfWork.CertificateRepository.Get(msg.RegistryName, msg.CertificateId);

        if (cert == null)
        {
            var granularCertificate = new GranularCertificate
            {
                Id = msg.CertificateId,
                RegistryName = msg.RegistryName,
                Type = msg.Type.MapToModel(),
                Quantity = msg.Quantity,
                Start = msg.Start,
                End = msg.End,
                GridArea = msg.GridArea,
                ClearTextAttributes = msg.ClearTextAttributes,
                HashedAttributes = new List<CertificateHashedAttribute>() //msg.HashedAttributes
            };

            await _unitOfWork.CertificateRepository.Create(granularCertificate);
            _unitOfWork.Commit();
        }

        var endpointPosition = WalletEndpointPositionCalculator.CalculateWalletEndpointPosition(msg.Start);
        if (!endpointPosition.HasValue)
            throw new WalletException($"Cannot determine wallet endpoint position for certificate with id {msg.CertificateId}");

        var recipient = await _unitOfWork.RecipientRepository.Get(msg.RecipientId);
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
            issueEvent = Helpers.Registry.CreateIssuedEventForConsumption(msg.RegistryName, msg.CertificateId, PeriodHelper.ToDateInterval(msg.Start, msg.End), msg.GridArea, recipient.GSRN, commitment, ownerPublicKey);
        }

        using var channel = GrpcChannel.ForAddress(_registryOptions.GetRegistryUrl(msg.RegistryName));
        var client = new RegistryService.RegistryServiceClient(channel);

        var request = new SendTransactionsRequest();
        request.Transactions.Add(issueEvent.CreateTransaction(issuerKey));

        await client.SendTransactionsAsync(request);

        //await context.Execute();
    }
}
