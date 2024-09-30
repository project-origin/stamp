using FluentAssertions;
using ProjectOrigin.Stamp.Repositories;
using Npgsql;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.Stamp.Models;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using Xunit;

namespace ProjectOrigin.Stamp.Test.Repositories;

public class RecipientRepositoryTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly RecipientRepository _repository;

    public RecipientRepositoryTests(PostgresDatabaseFixture dbFixture)
    {
        var connection = new NpgsqlConnection(dbFixture.ConnectionString);
        connection.Open();
        _repository = new RecipientRepository(connection);
    }

    [Fact]
    public async Task CreateAndQueryRecipient()
    {
        var privateKey = new Secp256k1Algorithm().GenerateNewPrivateKey();
        var recipient = new Recipient
        {
            Id = Guid.NewGuid(),
            WalletEndpointReferenceEndpoint = "http://foo",
            WalletEndpointReferencePublicKey = privateKey.Neuter(),
            WalletEndpointReferenceVersion = 1
        };

        await _repository.Create(recipient);

        var queriedRecipient = await _repository.Get(recipient.Id);

        queriedRecipient.Should().NotBeNull();

        queriedRecipient!.Id.Should().Be(recipient.Id);
        queriedRecipient.WalletEndpointReferenceVersion.Should().Be(recipient.WalletEndpointReferenceVersion);
        queriedRecipient.WalletEndpointReferenceEndpoint.Should().Be(recipient.WalletEndpointReferenceEndpoint);
        queriedRecipient.WalletEndpointReferencePublicKey.Export().ToArray().Should().BeEquivalentTo(recipient.WalletEndpointReferencePublicKey.Export().ToArray());
    }

    [Fact]
    public async Task GetNextWalletEndpointPosition()
    {
        var recipientId = Guid.NewGuid();
        var position = await _repository.GetNextWalletEndpointPosition(recipientId);

        position.Should().Be(1);

        position = await _repository.GetNextWalletEndpointPosition(recipientId);

        position.Should().Be(2);
    }
}
