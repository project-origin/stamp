using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Stamp.Server.Database;
using ProjectOrigin.Stamp.Server.Exceptions;
using ProjectOrigin.Stamp.Server.Models;

namespace ProjectOrigin.Stamp.Server.EventHandlers;

public record CertificateMarkedAsIssuedEvent
{
    public required Guid CertificateId { get; init; }
    public required string RegistryName { get; init; }
    public required Guid RecipientId { get; init; }
    public required uint WalletEndpointPosition { get; init; }
    public required uint Quantity { get; init; }
    public required byte[] RandomR { get; init; }
    public required List<CertificateHashedAttribute> HashedAttributes { get; init; }
}

public class CertificateMarkedAsIssuedEventHandler : IConsumer<CertificateMarkedAsIssuedEvent>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CertificateMarkedAsIssuedEventHandler> _logger;

    public CertificateMarkedAsIssuedEventHandler(IHttpClientFactory httpClientFactory, IUnitOfWork unitOfWork, ILogger<CertificateMarkedAsIssuedEventHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CertificateMarkedAsIssuedEvent> context)
    {
        var message = context.Message;
        var recipient = await _unitOfWork.RecipientRepository.Get(message.RecipientId);

        if (recipient == null)
        {
            _logger.LogError("Recipient with id {recipientId} not found.", message.RecipientId);
            throw new WalletException($"Recipient with id {message.RecipientId} not found.");
        }

        _logger.LogInformation("Sending slice to Wallet with url {WalletUrl} for certificate id {certificateId}.",
            recipient.WalletEndpointReferenceEndpoint, message.CertificateId);

        if (recipient.WalletEndpointReferenceVersion == 1)
        {
            var request = new WalletReceiveRequest()
            {
                CertificateId = new FederatedStreamId()
                {
                    Registry = message.RegistryName,
                    StreamId = message.CertificateId,
                },
                Position = message.WalletEndpointPosition,
                PublicKey = recipient.WalletEndpointReferencePublicKey.Export().ToArray(),
                Quantity = message.Quantity,
                RandomR = message.RandomR,
                HashedAttributes = message.HashedAttributes.Select(ha => new HashedAttribute()
                {
                    Key = ha.Key,
                    Value = ha.Value,
                    Salt = ha.Salt
                })
            };

            using var client = _httpClientFactory.CreateClient();
            var requestJson = JsonSerializer.Serialize(request);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var res = await client.PostAsync(recipient.WalletEndpointReferenceEndpoint, content);
            res.EnsureSuccessStatusCode();

            _logger.LogInformation("Slice sent to Wallet for certificate id {certificateId}.", message.CertificateId);
        }
        else
            throw new WalletException($"Unsupported WalletEndpointReference version: {recipient.WalletEndpointReferenceVersion}");
    }
}

/// <summary>
/// Request to receive a certificate-slice from another wallet.
/// </summary>
public record WalletReceiveRequest()
{
    /// <summary>
    /// The public key of the receiving wallet.
    /// </summary>
    public required byte[] PublicKey { get; init; }

    /// <summary>
    /// The sub-position of the publicKey used on the slice on the registry.
    /// </summary>
    public required uint Position { get; init; }

    /// <summary>
    /// The id of the certificate.
    /// </summary>
    public required FederatedStreamId CertificateId { get; init; }

    /// <summary>
    /// The quantity of the slice.
    /// </summary>
    public required uint Quantity { get; init; }

    /// <summary>
    /// The random R used to generate the pedersen commitment with the quantity.
    /// </summary>
    public required byte[] RandomR { get; init; }

    /// <summary>
    /// List of hashed attributes, their values and salts so the receiver can access the data.
    /// </summary>
    public required IEnumerable<HashedAttribute> HashedAttributes { get; init; }
}

public record FederatedStreamId()
{
    public required string Registry { get; init; }
    public required Guid StreamId { get; init; }
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

    /// <summary>
    /// The salt used to hash the attribute.
    /// </summary>
    public required byte[] Salt { get; init; }
}
