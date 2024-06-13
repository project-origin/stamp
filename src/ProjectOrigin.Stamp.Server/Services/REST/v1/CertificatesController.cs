using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.EventHandlers;
using ProjectOrigin.Stamp.Server.Extensions;
using ProjectOrigin.Stamp.Server.Helpers;
using ProjectOrigin.Stamp.Server.Models;

namespace ProjectOrigin.Stamp.Server.Services.REST.v1;

[ApiController]
public class CertificatesController : ControllerBase
{
    /// <summary>
    /// Queues a certificate for issuance.
    /// </summary>
    /// <param name="bus"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="request">The issue certificate request.</param>
    /// <response code="202">The certificate has been queued for issuance.</response>
    /// <response code="400">The start date of the certificate must be before the end date.</response>
    /// <response code="404">Recipient not found.</response>
    [HttpPost]
    [Route("v1/certificates")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> IssueCertificate(
        [FromServices] IBus bus,
        [FromServices] IUnitOfWork unitOfWork,
        [FromBody] CreateCertificateRequest request)
    {
        if (request.Certificate.Start >= request.Certificate.End)
            return BadRequest("Start date must be before end date.");

        if (WalletEndpointPositionCalculator.CalculateWalletEndpointPosition(request.Certificate.Start) == null)
            return BadRequest("Start date must be rounded to nearest minute.");

        var recipient = await unitOfWork.RecipientRepository.Get(request.RecipientId);

        if (recipient == null)
            return NotFound($"Recipient with id {request.RecipientId} not found.");

        var certificate = await unitOfWork.CertificateRepository.Get(request.RegistryName, request.Certificate.Id);

        if (certificate != null)
            return Conflict($"Certificate with registry {request.RegistryName} and certificateId {request.Certificate.Id} already exists.");
        else
        {
            certificate = new GranularCertificate
            {
                Id = request.Certificate.Id,
                RegistryName = request.RegistryName,
                CertificateType = request.Certificate.Type.MapToModel(),
                Quantity = request.Certificate.Quantity,
                StartDate = request.Certificate.Start,
                EndDate = request.Certificate.End,
                GridArea = request.Certificate.GridArea,
                ClearTextAttributes = request.Certificate.ClearTextAttributes,
                HashedAttributes = request.Certificate.HashedAttributes.Select(ha => new CertificateHashedAttribute
                {
                    HaKey = ha.Key,
                    HaValue = ha.Value,
                    Salt = Guid.NewGuid().ToByteArray()
                }).ToList()
            };

            await unitOfWork.CertificateRepository.Create(certificate);
        }

        var payloadObj = new CertificateCreatedEvent
        {
            CertificateId = certificate.Id,
            CertificateType = certificate.CertificateType,
            Start = certificate.StartDate,
            End = certificate.EndDate,
            GridArea = certificate.GridArea,
            ClearTextAttributes = certificate.ClearTextAttributes,
            HashedAttributes = certificate.HashedAttributes,
            Quantity = certificate.Quantity,
            RegistryName = request.RegistryName,
            WalletEndpointReferencePublicKey = recipient.WalletEndpointReferencePublicKey.Export().ToArray(),
            RecipientId = request.RecipientId
        };
        await unitOfWork.OutboxMessageRepository.Create(new OutboxMessage
        {
            Created = DateTimeOffset.Now.ToUtcTime(),
            Id = Guid.NewGuid(),
            JsonPayload = JsonSerializer.Serialize(payloadObj),
            MessageType = typeof(CertificateCreatedEvent).ToString()
        });
        unitOfWork.Commit();

        return Accepted(new IssueCertificateResponse());
    }
}

#region Records

public record CreateCertificateRequest
{
    /// <summary>
    /// The recipient id of the certificate.
    /// </summary>
    public required Guid RecipientId { get; init; }

    /// <summary>
    /// The registry used to issues the certificate.
    /// </summary>
    public required string RegistryName { get; init; }

    /// <summary>
    /// The certificate to issue.
    /// </summary>
    public required CertificateDto Certificate { get; init; }
}

public record CertificateDto
{
    /// <summary>
    /// The id of the certificate.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The type of certificate (production or consumption).
    /// </summary>
    public required CertificateType Type { get; init; }

    /// <summary>
    /// The quantity available on the certificate.
    /// </summary>
    public required uint Quantity { get; init; }

    /// <summary>
    /// The start of the period for which the certificate is valid.
    /// </summary>
    public required long Start { get; init; }

    /// <summary>
    /// The end of the period for which the certificate is valid.
    /// </summary>
    public required long End { get; init; }

    /// <summary>
    /// The Grid Area of the certificate.
    /// </summary>
    public required string GridArea { get; init; }

    /// <summary>
    /// Attributes of the certificate that is not hashed.
    /// </summary>
    public required Dictionary<string, string> ClearTextAttributes { get; init; }

    /// <summary>
    /// List of hashed attributes, their values and salts so the receiver can access the data.
    /// </summary>
    public required IEnumerable<HashedAttribute> HashedAttributes { get; init; }
}

public enum CertificateType
{
    Consumption = 1,
    Production = 2
}

/// <summary>
/// Hashed attribute with salt.
/// </summary>
public record HashedAttribute()
{
    /// <summary>
    /// The key of the attribute.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// The value of the attribute.
    /// </summary>
    public required string Value { get; init; }
}

/// <summary>
/// Response to issue certificate request.
/// </summary>
public record IssueCertificateResponse() { }

#endregion
