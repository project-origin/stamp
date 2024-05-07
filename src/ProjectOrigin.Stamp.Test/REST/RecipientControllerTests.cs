using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.Stamp.Server;
using ProjectOrigin.Stamp.Server.Services.REST.v1;
using ProjectOrigin.Stamp.Test.Extensions;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using Xunit;

namespace ProjectOrigin.Stamp.Test.REST;

public class RecipientControllerTests : IClassFixture<TestServerFixture<Startup>>, IClassFixture<PostgresDatabaseFixture>
{
    private readonly TestServerFixture<Startup> _fixture;

    public RecipientControllerTests(TestServerFixture<Startup> fixture, PostgresDatabaseFixture postgres)
    {
        fixture.PostgresConnectionString = postgres.ConnectionString;
        _fixture = fixture;
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

        var get = await client.GetFromJsonAsync<RecipientDto>(post.Headers.Location!.ToString());

        get.Should().NotBeNull();
        get!.Id.Should().Be(response!.Id);
        get.WalletEndpointReference.Version.Should().Be(createRecipientRequest.WalletEndpointReference.Version);
        get.WalletEndpointReference.Endpoint.Should().Be(createRecipientRequest.WalletEndpointReference.Endpoint);
        get.WalletEndpointReference.PublicKey.Should().BeEquivalentTo(createRecipientRequest.WalletEndpointReference.PublicKey);
    }
}
