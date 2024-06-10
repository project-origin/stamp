using MassTransit;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.EventHandlers;
using ProjectOrigin.Stamp.Server.Models;
using ProjectOrigin.Stamp.Server.Extensions;
using Microsoft.Extensions.Options;
using ProjectOrigin.Stamp.Server.Options;

namespace ProjectOrigin.Stamp.Server.CommandHandlers;

public record CreateCertificateCommand
{
    public required Guid RecipientId { get; init; }
    public required byte[] WalletEndpointReferencePublicKey { get; init; }
    public required string RegistryName { get; init; }
    public required Guid CertificateId { get; init; }
    public required GranularCertificateType CertificateType { get; init; }
    public required uint Quantity { get; init; }
    public required long Start { get; init; }
    public required long End { get; init; }
    public required string GridArea { get; init; }
    public required Dictionary<string, string> ClearTextAttributes { get; init; }
    public required List<CertificateHashedAttribute> HashedAttributes { get; init; }
}

public class CreateCertificateCommandHandler : IConsumer<CreateCertificateCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateCertificateCommandHandler> _logger;

    public CreateCertificateCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateCertificateCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateCertificateCommand> context)
    {
        _logger.LogInformation("Creating certificate with id {certificateId}.", context.Message.CertificateId);
        var message = context.Message;

        var cert = await _unitOfWork.CertificateRepository.Get(message.RegistryName, message.CertificateId);

        if (cert == null)
        {
            cert = new GranularCertificate
            {
                Id = message.CertificateId,
                RegistryName = message.RegistryName,
                CertificateType = message.CertificateType,
                Quantity = message.Quantity,
                StartDate = message.Start,
                EndDate = message.End,
                GridArea = message.GridArea,
                ClearTextAttributes = message.ClearTextAttributes,
                HashedAttributes = message.HashedAttributes
            };

            await _unitOfWork.CertificateRepository.Create(cert);
        }

        var payloadObj = new CertificateCreatedEvent
        {
            CertificateId = cert.Id,
            //CertificateType = cert.CertificateType,
            //Start = cert.StartDate,
            //End = cert.EndDate,
            //GridArea = cert.GridArea,
            //ClearTextAttributes = cert.ClearTextAttributes,
            //HashedAttributes = cert.HashedAttributes,
            //Quantity = cert.Quantity,
            //RegistryName = message.RegistryName,
            //WalletEndpointReferencePublicKey = message.WalletEndpointReferencePublicKey,
            //RecipientId = message.RecipientId
        };
        await _unitOfWork.OutboxMessageRepository.Create(new OutboxMessage
        {
            Created = DateTimeOffset.Now.ToUtcTime(),
            Id = Guid.NewGuid(),
            JsonPayload = JsonSerializer.Serialize(payloadObj),
            MessageType = typeof(CertificateCreatedEvent).ToString()
        });
        _unitOfWork.Commit();
    }
}

public class CreateCertificateCommandHandlerDefinition : ConsumerDefinition<CreateCertificateCommandHandler>
{
    private readonly RetryOptions _retryOptions;

    public CreateCertificateCommandHandlerDefinition(IOptions<RetryOptions> options)
    {
        _retryOptions = options.Value;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CreateCertificateCommandHandler> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(_retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
