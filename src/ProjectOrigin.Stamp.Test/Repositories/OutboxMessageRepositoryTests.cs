using System.Text.Json;
using FluentAssertions;
using Npgsql;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.Stamp.Server.EventHandlers;
using ProjectOrigin.Stamp.Server.Extensions;
using ProjectOrigin.Stamp.Server.Models;
using ProjectOrigin.Stamp.Server.Repositories;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using Xunit;

namespace ProjectOrigin.Stamp.Test.Repositories;

public class OutboxMessageRepositoryTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly OutboxMessageRepository _repository;

    public OutboxMessageRepositoryTests(PostgresDatabaseFixture dbFixture)
    {
        var connection = new NpgsqlConnection(dbFixture.ConnectionString);
        connection.Open();
        _repository = new OutboxMessageRepository(connection);
    }

    [Fact]
    public async Task CreateGetAndParseOutboxMessage()
    {
        var privateKey = new Secp256k1Algorithm().GenerateNewPrivateKey();
        var payloadObj = new CertificateCreatedEvent
        {
            CertificateId = Guid.NewGuid(),
            CertificateType = GranularCertificateType.Production,
            Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            GridArea = "DK1",
            ClearTextAttributes = new Dictionary<string, string> { { "TechCode", "T12345" } },
            HashedAttributes = new List<CertificateHashedAttribute>(),
            Quantity = 1234,
            RegistryName = "Energinet.dk",
            WalletEndpointReferencePublicKey = privateKey.Neuter().Export().ToArray(),
            RecipientId = Guid.NewGuid()
        };
        var message = new OutboxMessage
        {
            Created = DateTimeOffset.Now.ToUtcTime(),
            JsonPayload = JsonSerializer.Serialize(payloadObj),
            MessageType = typeof(CertificateCreatedEvent).ToString(),
            Id = Guid.NewGuid()
        };

        await _repository.Create(message);

        var queriedMessage = await _repository.GetFirstNonProcessed();

        queriedMessage.Should().BeEquivalentTo(message);

        var type = Type.GetType($"{queriedMessage!.MessageType}, ProjectOrigin.Stamp.Server");
        var loadedObject = JsonSerializer.Deserialize(queriedMessage.JsonPayload, type!);
        loadedObject.Should().BeEquivalentTo(payloadObj);
    }

    [Fact]
    public async Task Delete()
    {
        var message = new OutboxMessage
        {
            Created = DateTimeOffset.Now.ToUtcTime(),
            JsonPayload = "{}",
            MessageType = "Test",
            Id = Guid.NewGuid()
        };

        await _repository.Create(message);

        var queriedMessage = await _repository.GetFirstNonProcessed();
        queriedMessage.Should().BeEquivalentTo(message);

        await _repository.Delete(queriedMessage!.Id);

        var deletedMessage = await _repository.GetFirstNonProcessed();
        deletedMessage.Should().BeNull();
    }
}
