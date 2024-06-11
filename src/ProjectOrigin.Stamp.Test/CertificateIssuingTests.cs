using ProjectOrigin.Stamp.Server;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using FluentAssertions;
using ProjectOrigin.Stamp.Server.Services.REST.v1;
using ProjectOrigin.Stamp.Test.Extensions;
using Xunit;

namespace ProjectOrigin.Stamp.Test;

public class CertificateIssuingTests : IClassFixture<TestServerFixture<Startup>>,
    IClassFixture<PostgresDatabaseFixture>,
    IClassFixture<ProjectOriginStack>,
    IClassFixture<RabbitMqContainer>
{
    private readonly TestServerFixture<Startup> _fixture;
    private readonly ProjectOriginStack _poStack;

    public CertificateIssuingTests(TestServerFixture<Startup> fixture, PostgresDatabaseFixture postgres, ProjectOriginStack poStack, RabbitMqContainer rabbitMq)
    {
        _fixture = fixture;
        _poStack = poStack;
        fixture.PostgresConnectionString = postgres.ConnectionString;
        fixture.RabbitMqOptions = rabbitMq.Options;
        fixture.RegistryOptions = poStack.RegistryOptions;
    }

    [Fact]
    public async Task NoCertificates()
    {
        var client = _poStack.CreateWalletClient(Guid.NewGuid().ToString());

        var certs = await client.QueryCertificates();

        certs.Should().BeEmpty();
    }

    [Fact]
    public async Task IssueProductionCertificate()
    {
        var client = _fixture.CreateHttpClient();

        var recipientId = await client.AddRecipient(_poStack.WalletReceiveSliceUrl);

        var cert = new CertificateDto
        {
            Id = Guid.NewGuid(),
            Start = DateTimeOffset.UtcNow.RoundToLatestHour(),
            End = DateTimeOffset.UtcNow.AddHours(1).RoundToLatestHour(),
            GridArea = _fixture.RegistryOptions.IssuerPrivateKeyPems.First().Key,
            Quantity = 1234,
            Type = Server.Services.REST.v1.CertificateType.Production,
            ClearTextAttributes = new Dictionary<string, string>
            {
                { "fuelCode", "F01040100" },
                { "techCode", "T010000" }
            },
            HashedAttributes = new List<HashedAttribute>
            {
                new () { Key = "assetId", Value = "", Salt = new byte[2] }
            }
        };

        await client.PostCertificate(recipientId, _fixture.RegistryOptions.RegistryUrls.First().Key, cert);

        var walletClient = _poStack.CreateWalletClient(Guid.NewGuid().ToString());

        var certs = await walletClient.RepeatedlyQueryCertificatesUntil(certs => certs.Any());

        certs.Should().HaveCount(1);

    }
}
