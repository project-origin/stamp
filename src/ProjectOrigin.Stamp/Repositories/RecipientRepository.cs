using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ProjectOrigin.Stamp.Models;

namespace ProjectOrigin.Stamp.Repositories;

public interface IRecipientRepository
{
    Task<int> Create(Recipient recipient);
    Task<Recipient?> Get(Guid id);
    Task<uint> GetNextWalletEndpointPosition(Guid recipientId);
}

public class RecipientRepository : IRecipientRepository
{
    private readonly IDbConnection _connection;

    public RecipientRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public Task<int> Create(Recipient recipient)
    {
        return _connection.ExecuteAsync(
            @"INSERT INTO recipients (id, wallet_endpoint_reference_version, wallet_endpoint_reference_endpoint, wallet_endpoint_reference_public_key)
                VALUES (@Id, @WalletEndpointReferenceVersion, @WalletEndpointReferenceEndpoint, @WalletEndpointReferencePublicKey)",
            new
            {
                recipient.Id,
                recipient.WalletEndpointReferenceVersion,
                recipient.WalletEndpointReferenceEndpoint,
                recipient.WalletEndpointReferencePublicKey
            });
    }

    public Task<Recipient?> Get(Guid id)
    {
        return _connection.QueryFirstOrDefaultAsync<Recipient>(
            @"SELECT id, wallet_endpoint_reference_version, wallet_endpoint_reference_endpoint, wallet_endpoint_reference_public_key
                FROM recipients
                WHERE id = @Id",
            new { Id = id });
    }

    public Task<uint> GetNextWalletEndpointPosition(Guid recipientId)
    {
        return _connection.ExecuteScalarAsync<uint>(
            @"SELECT *
              FROM IncrementNumberForId(@id);",
            new
            {
                id = recipientId
            });
    }
}
