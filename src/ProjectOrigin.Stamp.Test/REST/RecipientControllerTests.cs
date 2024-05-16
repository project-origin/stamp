using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.Stamp.Server;
using ProjectOrigin.Stamp.Server.Repositories;
using ProjectOrigin.Stamp.Server.Services.REST.v1;
using ProjectOrigin.Stamp.Test.Extensions;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using Xunit;

namespace ProjectOrigin.Stamp.Test.REST;

public class RecipientControllerTests : IClassFixture<TestServerFixture<Startup>>, IClassFixture<PostgresDatabaseFixture>
{
    private readonly TestServerFixture<Startup> _fixture;
    private readonly PostgresDatabaseFixture _postgres;

    public RecipientControllerTests(TestServerFixture<Startup> fixture, PostgresDatabaseFixture postgres)
    {
        fixture.PostgresConnectionString = postgres.ConnectionString;
        _fixture = fixture;
        _postgres = postgres;
    }

    [Fact]
    public async Task CreateAndGetRecipient()
    {
        using var client = _fixture.CreateHttpClient();

        var createRecipientRequest = new CreateRecipientRequest
        {
            WalletEndpointReference = new WalletEndpointReferenceDto
            {
                Endpoint = new Uri("http://foo"),
                PublicKey = new Secp256k1Algorithm().GenerateNewPrivateKey().Neuter().Export().ToArray(),
                Version = 1
            }
        };

        var post = await client.PostAsJsonAsync("stamp-api/v1/recipients", createRecipientRequest);
        post.StatusCode.Should().Be(HttpStatusCode.Created);
        var response = await post.Content.ReadJson<CreateRecipientResponse>();

        using var connection = _postgres.GetConnectionFactory().CreateConnection();
        connection.Open();
        var repo = new RecipientRepository(connection);

        var recipient = await repo.Get(response!.Id);
        recipient!.WalletEndpointReferenceVersion.Should().Be(createRecipientRequest.WalletEndpointReference.Version);
        recipient.WalletEndpointReferenceEndpoint.Should().Be(createRecipientRequest.WalletEndpointReference.Endpoint.ToString());
        recipient.WalletEndpointReferencePublicKey.Export().ToArray().Should().BeEquivalentTo(createRecipientRequest.WalletEndpointReference.PublicKey);
    }
}
