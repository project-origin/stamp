using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Helpers;
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
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> WithdrawCertificate(
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] IOptions<RegistryOptions> registryOptions,
        [FromRoute] string registry,
        Guid certificateId)
    {

        var certificate = await unitOfWork.CertificateRepository.Get(registry, certificateId);
        if (certificate == null)
            return NotFound("Certificate not found.");

        var withdrawnCertificate = await unitOfWork.WithdrawnCertificateRepository.Get(registry, certificateId);
        if (withdrawnCertificate != null)
            return Conflict("Certificate already withdrawn.");


        var withdrawnEvent = new WithdrawnEvent();
        var issuerKey = registryOptions.Value.GetIssuerKey(certificate.GridArea);
        var transaction = withdrawnEvent.CreateTransaction(registry, certificateId, issuerKey);

        var request = new SendTransactionsRequest();
        request.Transactions.Add(transaction);

        using var channel = GrpcChannel.ForAddress(registryOptions.Value.GetRegistryUrl(registry));
        var client = new RegistryService.RegistryServiceClient(channel);
        await client.SendTransactionsAsync(request);

        await unitOfWork.WithdrawnCertificateRepository.Create(registry, certificateId);
        unitOfWork.Commit();

        return Accepted(new WithdrawnCertificateResponse
        {
            RegistryName = registry,
            CertificateId = certificateId
        });
    }
}

public record WithdrawnCertificateResponse
{
    public required string RegistryName { get; init; }
    public required Guid CertificateId { get; init; }
}
