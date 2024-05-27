using Dapper;
using ProjectOrigin.Stamp.Server.Models;
using System;
using System.Data;
using System.Threading.Tasks;

namespace ProjectOrigin.Stamp.Server.Repositories;

public interface IOutboxMessageRepository
{
    Task Create(OutboxMessage message);
    Task<OutboxMessage?> GetFirstNonProcessed();
    Task Delete(Guid outboxMessageId);
}

public class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly IDbConnection _connection;

    public OutboxMessageRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task Create(OutboxMessage message)
    {
        await _connection.ExecuteAsync(
            @"INSERT INTO OutboxMessages(id, message_type, json_payload, created, processed)
              VALUES (@Id, @MessageType, @JsonPayload, @Created, @Processed)",
            new
            {
                message.Id,
                message.MessageType,
                message.JsonPayload,
                message.Created,
                message.Processed
            });
    }

    public Task<OutboxMessage?> GetFirstNonProcessed()
    {
        return _connection.QueryFirstOrDefaultAsync<OutboxMessage>(
            @"SELECT id, message_type, json_payload, created, processed
                FROM OutboxMessages
                WHERE processed = false");
    }

    public async Task Delete(Guid outboxMessageId)
    {
        var rowsChanged = await _connection.ExecuteAsync(
            @"DELETE FROM OutboxMessages
                WHERE id = @Id",
            new
            {
                Id = outboxMessageId
            });

        if (rowsChanged != 1)
            throw new InvalidOperationException($"OutboxMessage with id {outboxMessageId} could not be found");
    }
}
