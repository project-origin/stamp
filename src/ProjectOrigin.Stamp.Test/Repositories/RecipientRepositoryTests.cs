using FluentAssertions;
using ProjectOrigin.Stamp.Server.Repositories;
using Npgsql;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.Stamp.Server.Models;
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
}
