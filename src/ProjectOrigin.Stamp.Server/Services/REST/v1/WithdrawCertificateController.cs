using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Helpers;
using ProjectOrigin.Stamp.Server.Models;
using ProjectOrigin.Stamp.Server.Options;

namespace ProjectOrigin.Stamp.Server.Services.REST.v1;

[ApiController]
public class WithdrawnCertificatesController : ControllerBase
{
    /// <summary>
    /// Withdraw a certificate
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="registryOptions"></param>
    /// <param name="registry"></param>
    /// <param name="certificateId"></param>
    /// <response code="202">The certificate has been withdrawn.</response>
    /// <response code="400">The certificate was not found in the database.</response>
    /// <response code="404">The certificate was already withdrawn.</response>
    [HttpPost]
    [Route("v1/certificates/{registry}/{certificateId}/withdraw")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> WithdrawCertificate(
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] IOptions<RegistryOptions> registryOptions,
        [FromRoute] string registry,
        Guid certificateId)
    {
        var withdrawnCertificate = await unitOfWork.WithdrawnCertificateRepository.Get(registry, certificateId);
        if (withdrawnCertificate != null)
            return Conflict("Certificate already withdrawn.");

        var certificate = await unitOfWork.CertificateRepository.Get(registry, certificateId);
        if (certificate == null)
            return NotFound("Certificate not found.");

        var withdrawnEvent = new WithdrawnEvent();
        var issuerKey = registryOptions.Value.GetIssuerKey(certificate.GridArea);
        var transaction = withdrawnEvent.CreateTransaction(registry, certificateId, issuerKey);

        var request = new SendTransactionsRequest();
        request.Transactions.Add(transaction);

        using var channel = GrpcChannel.ForAddress(registryOptions.Value.GetRegistryUrl(registry));
        var client = new RegistryService.RegistryServiceClient(channel);
        await client.SendTransactionsAsync(request);

        var createdWithdrawnCertificate = await unitOfWork.WithdrawnCertificateRepository.Withdraw(certificate);
        unitOfWork.Commit();

        return Created(string.Empty, new WithdrawnCertificateResponse
        {
            Id = createdWithdrawnCertificate.Id,
            RegistryName = registry,
            CertificateId = certificateId,
            WithdrawnDate = createdWithdrawnCertificate.WithdrawnDate
        });
    }

    /// <summary>
    /// Withdraw a certificate
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="lastWithdrawnId">The id of the withdrawn certificate you want to start from.
    /// Ie if set to 100 you get withdrawn certificates starting from number 100.</param>
    /// <param name="skip">The number of items to skip.</param>
    /// <param name="limit">The number of items to return.</param>
    /// <response code="200">Returns the withdrawn certificates.</response>
    [HttpGet]
    [Route("v1/certificates/withdrawn")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultList<WithdrawnCertificateDto, PageInfo>>> WithdrawnCertificates(
        [FromServices] IUnitOfWork unitOfWork,
        [FromQuery] int lastWithdrawnId = 0,
        int skip = 0,
        int? limit = null)
    {
        limit ??= int.MaxValue;
        var withdrawnCertificates = await unitOfWork.WithdrawnCertificateRepository.GetMultiple(lastWithdrawnId, skip, limit.Value);

        return Ok(withdrawnCertificates.ToResultList(wc => wc.MapToV1()));
    }
}

public record WithdrawnCertificateResponse
{
    public required int Id { get; init; }
    public required string RegistryName { get; init; }
    public required Guid CertificateId { get; init; }
    public required DateTimeOffset WithdrawnDate { get; init; }
}

public record WithdrawnCertificateDto
{
    public required int Id { get; init; }
    public required string RegistryName { get; init; }
    public required Guid CertificateId { get; init; }
    public required DateTimeOffset WithdrawnDate { get; init; }
}
