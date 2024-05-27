using MassTransit;
using ProjectOrigin.Stamp.Server.Services.REST.v1;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.EventHandlers;
using ProjectOrigin.Stamp.Server.Models;

namespace ProjectOrigin.Stamp.Server.CommandHandlers;

public record CreateCertificateCommand
{
    public required Guid RecipientId { get; init; }
    public required string RegistryName { get; init; }
    public required Guid CertificateId { get; init; }
    public required GranularCertificateType CertificateType { get; init; }
    public required uint Quantity { get; init; }
    public required long Start { get; init; }
    public required long End { get; init; }
    public required string GridArea { get; init; }
    public required Dictionary<string, string> ClearTextAttributes { get; init; }
    public required IEnumerable<HashedAttribute> HashedAttributes { get; init; }
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
        var msg = context.Message;
        //TODO: Get from db and if not present: save to db (if GSRN is available)
        //TODO: Skal assetId med i dto?
        //TODO: Set HashedAttributes

        var cert = await _unitOfWork.CertificateRepository.Get(msg.RegistryName, msg.CertificateId);

        if (cert == null)
        {
            var granularCertificate = new GranularCertificate
            {
                Id = msg.CertificateId,
                RegistryName = msg.RegistryName,
                CertificateType = msg.CertificateType,
                Quantity = msg.Quantity,
                StartDate = msg.Start,
                EndDate = msg.End,
                GridArea = msg.GridArea,
                ClearTextAttributes = msg.ClearTextAttributes,
                HashedAttributes = new List<CertificateHashedAttribute>() //msg.HashedAttributes
            };

            await _unitOfWork.CertificateRepository.Create(granularCertificate);
        }

        //await context.Publish<CertificateCreatedEvent>(new CertificateCreatedEvent
        //{

        //});
        _unitOfWork.Commit();
    }
}
