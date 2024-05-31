using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Models;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using Microsoft.AspNetCore.Http;

namespace ProjectOrigin.Stamp.Server.Services.REST.v1;

[ApiController]
public class RecipientController : ControllerBase
{
    /// <summary>
    /// Creates a new recipient
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="request">The create recipient request</param>
    /// <response code="201">The recipient was created.</response>
    [HttpPost]
    [Route("v1/recipients")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateRecipientResponse>> CreateRecipient(
        [FromServices] IUnitOfWork unitOfWork,
        [FromBody] CreateRecipientRequest request)
    {
        if(request.WalletEndpointReference.Version != 1)
            return BadRequest("We currently only support Wallet endpoint reference version 1.");

        var recipient = new Recipient
        {
            Id = Guid.NewGuid(),
            WalletEndpointReferenceVersion = request.WalletEndpointReference.Version,
            WalletEndpointReferenceEndpoint = request.WalletEndpointReference.Endpoint.ToString(),
            WalletEndpointReferencePublicKey = new Secp256k1Algorithm().ImportHDPublicKey(request.WalletEndpointReference.PublicKey)
        };

        await unitOfWork.RecipientRepository.Create(recipient);

        unitOfWork.Commit();

        return Created(null as string, new CreateRecipientResponse
        {
            Id = recipient.Id
        });
    }
}

#region Records

/// <summary>
/// Request to create a new recipient.
/// </summary>
public record CreateRecipientRequest
{
    /// <summary>
    /// The recipient wallet endpoint reference.
    /// </summary>
    public required WalletEndpointReferenceDto WalletEndpointReference { get; init; }
}

public record WalletEndpointReferenceDto
{
    /// <summary>
    /// The version of the ReceiveSlice API.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// The url endpoint of where the wallet is hosted.
    /// </summary>
    public required Uri Endpoint { get; init; }

    /// <summary>
    /// The public key used to generate sub-public-keys for each slice.
    /// </summary>
    public required byte[] PublicKey { get; init; }
}

/// <summary>
/// Response to create a recipient.
/// </summary>
public record CreateRecipientResponse
{
    /// <summary>
    /// The ID of the created recipient.
    /// </summary>
    public required Guid Id { get; init; }
}
#endregion
