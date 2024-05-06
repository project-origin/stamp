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

        Assert.NotNull(queriedRecipient);
        Assert.Equal(recipient.Id, queriedRecipient.Id);
        Assert.Equal(recipient.WalletEndpointReferenceVersion, queriedRecipient.WalletEndpointReferenceVersion);
        Assert.Equal(recipient.WalletEndpointReferenceEndpoint, queriedRecipient.WalletEndpointReferenceEndpoint);
        Assert.Equal(recipient.WalletEndpointReferencePublicKey.Export(), queriedRecipient.WalletEndpointReferencePublicKey.Export());
    }
}
