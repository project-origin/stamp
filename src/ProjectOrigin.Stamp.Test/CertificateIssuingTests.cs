using System.Net;
using ProjectOrigin.Stamp.Server;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using FluentAssertions;
using ProjectOrigin.Stamp.Server.Services.REST.v1;
using ProjectOrigin.Stamp.Test.Extensions;
using Xunit;
using CertificateType = ProjectOrigin.Stamp.Server.Services.REST.v1.CertificateType;

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
    public async Task WhenEndDateIsBeforeStartDate_BadRequest()
    {
        var walletClient = _poStack.CreateWalletClient(Guid.NewGuid().ToString());

        var endpointRef = await walletClient.CreateWalletAndEndpoint();

        var client = _fixture.CreateHttpClient();
        var recipientId = await client.AddRecipient(endpointRef);

        var gsrn = Some.Gsrn();
        var cert = new CertificateDto
        {
            Id = Guid.NewGuid(),
            Start = DateTimeOffset.UtcNow.AddHours(1).RoundToLatestHourLong(),
            End = DateTimeOffset.UtcNow.RoundToLatestHourLong(),
            GridArea = _fixture.RegistryOptions.IssuerPrivateKeyPems.First().Key,
            Quantity = 1234,
            Type = CertificateType.Production,
            ClearTextAttributes = new Dictionary<string, string>
            {
                { "fuelCode", "F01040100" },
                { "techCode", "T010000" }
            },
            HashedAttributes = new List<HashedAttribute>
            {
                new () { Key = "assetId", Value = gsrn }
            }
        };

        var response = await client.PostCertificate(recipientId, _fixture.RegistryOptions.RegistryUrls.First().Key, gsrn, cert);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CannotDetermineWalletEndpointPosition_BadRequest()
    {
        var walletClient = _poStack.CreateWalletClient(Guid.NewGuid().ToString());

        var endpointRef = await walletClient.CreateWalletAndEndpoint();

        var client = _fixture.CreateHttpClient();
        var recipientId = await client.AddRecipient(endpointRef);

        var gsrn = Some.Gsrn();
        var cert = new CertificateDto
        {
            Id = Guid.NewGuid(),
            Start = DateTimeOffset.UtcNow.RoundToLatestHour().AddSeconds(32).ToUnixTimeSeconds(),
            End = DateTimeOffset.UtcNow.AddHours(1).RoundToLatestHourLong(),
            GridArea = _fixture.RegistryOptions.IssuerPrivateKeyPems.First().Key,
            Quantity = 1234,
            Type = CertificateType.Production,
            ClearTextAttributes = new Dictionary<string, string>
            {
                { "fuelCode", "F01040100" },
                { "techCode", "T010000" }
            },
            HashedAttributes = new List<HashedAttribute>
            {
                new () { Key = "assetId", Value = gsrn }
            }
        };

        var response = await client.PostCertificate(recipientId, _fixture.RegistryOptions.RegistryUrls.First().Key, gsrn, cert);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenCertificateWithMeteringPointIdStartAndEndAlreadyExists()
    {
        var walletClient = _poStack.CreateWalletClient(Guid.NewGuid().ToString());

        var endpointRef = await walletClient.CreateWalletAndEndpoint();

        var client = _fixture.CreateHttpClient();
        var recipientId = await client.AddRecipient(endpointRef);

        var gsrn = Some.Gsrn();
        var cert = new CertificateDto
        {
            Id = Guid.NewGuid(),
            Start = DateTimeOffset.UtcNow.RoundToLatestHourLong(),
            End = DateTimeOffset.UtcNow.AddHours(1).RoundToLatestHourLong(),
            GridArea = _fixture.RegistryOptions.IssuerPrivateKeyPems.First().Key,
            Quantity = 1234,
            Type = CertificateType.Production,
            ClearTextAttributes = new Dictionary<string, string>
            {
                { "fuelCode", "F01040100" },
                { "techCode", "T010000" }
            },
            HashedAttributes = new List<HashedAttribute>
            {
                new () { Key = "assetId", Value = gsrn }
            }
        };

        var response = await client.PostCertificate(recipientId, _fixture.RegistryOptions.RegistryUrls.First().Key, gsrn, cert);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var cert2 = new CertificateDto
        {
            Id = Guid.NewGuid(),
            Start = cert.Start,
            End = cert.End,
            GridArea = _fixture.RegistryOptions.IssuerPrivateKeyPems.First().Key,
            Quantity = 1234,
            Type = CertificateType.Production,
            ClearTextAttributes = new Dictionary<string, string>
            {
                { "fuelCode", "F01040100" },
                { "techCode", "T010000" }
            },
            HashedAttributes = new List<HashedAttribute>
            {
                new () { Key = "assetId", Value = gsrn }
            }
        };

        var response2 = await client.PostCertificate(recipientId, _fixture.RegistryOptions.RegistryUrls.First().Key, gsrn, cert2);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task WhenCertificateAlreadyExists_Conflict()
    {
        var walletClient = _poStack.CreateWalletClient(Guid.NewGuid().ToString());

        var endpointRef = await walletClient.CreateWalletAndEndpoint();

        var client = _fixture.CreateHttpClient();
        var recipientId = await client.AddRecipient(endpointRef);

        var gsrn = Some.Gsrn();
        var cert = new CertificateDto
        {
            Id = Guid.NewGuid(),
            Start = DateTimeOffset.UtcNow.RoundToLatestHourLong(),
            End = DateTimeOffset.UtcNow.AddHours(1).RoundToLatestHourLong(),
            GridArea = _fixture.RegistryOptions.IssuerPrivateKeyPems.First().Key,
            Quantity = 1234,
            Type = CertificateType.Production,
            ClearTextAttributes = new Dictionary<string, string>
            {
                { "fuelCode", "F01040100" },
                { "techCode", "T010000" }
            },
            HashedAttributes = new List<HashedAttribute>
            {
                new () { Key = "assetId", Value = gsrn }
            }
        };

        var response = await client.PostCertificate(recipientId, _fixture.RegistryOptions.RegistryUrls.First().Key, gsrn, cert);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        response = await client.PostCertificate(recipientId, _fixture.RegistryOptions.RegistryUrls.First().Key, "1234", cert);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task WhenNonExistingRecipient_NotFound()
    {
        var client = _fixture.CreateHttpClient();

        var gsrn = Some.Gsrn();
        var cert = new CertificateDto
        {
            Id = Guid.NewGuid(),
            Start = DateTimeOffset.UtcNow.RoundToLatestHourLong(),
            End = DateTimeOffset.UtcNow.AddHours(1).RoundToLatestHourLong(),
            GridArea = _fixture.RegistryOptions.IssuerPrivateKeyPems.First().Key,
            Quantity = 1234,
            Type = CertificateType.Production,
            ClearTextAttributes = new Dictionary<string, string>
            {
                { "fuelCode", "F01040100" },
                { "techCode", "T010000" }
            },
            HashedAttributes = new List<HashedAttribute>
            {
                new () { Key = "assetId", Value = gsrn }
            }
        };

        var response = await client.PostCertificate(Guid.NewGuid(), _fixture.RegistryOptions.RegistryUrls.First().Key, gsrn, cert);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(CertificateType.Production)]
    [InlineData(CertificateType.Consumption)]
    public async Task IssueCertificate(CertificateType type)
    {
        var walletClient = _poStack.CreateWalletClient(Guid.NewGuid().ToString());

        var endpointRef = await walletClient.CreateWalletAndEndpoint();

        var client = _fixture.CreateHttpClient();
        var recipientId = await client.AddRecipient(endpointRef);

        var gsrn = Some.Gsrn();
        var cert = new CertificateDto
        {
            Id = Guid.NewGuid(),
            Start = DateTimeOffset.UtcNow.RoundToLatestHourLong(),
            End = DateTimeOffset.UtcNow.AddHours(1).RoundToLatestHourLong(),
            GridArea = _fixture.RegistryOptions.IssuerPrivateKeyPems.First().Key,
            Quantity = 1234,
            Type = type,
            ClearTextAttributes = new Dictionary<string, string>
            {
                { "fuelCode", "F01040100" },
                { "techCode", "T010000" }
            },
            HashedAttributes = new List<HashedAttribute>
            {
                new () { Key = "assetId", Value = gsrn }
            }
        };

        var response = await client.PostCertificate(recipientId, _fixture.RegistryOptions.RegistryUrls.First().Key, gsrn, cert);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var certs = await walletClient.RepeatedlyGetCertificatesUntil(certs => certs.Any());

        certs.Should().HaveCount(1);
        var queriedCert = certs.First();

        queriedCert.FederatedStreamId.StreamId.Should().Be(cert.Id);
        queriedCert.FederatedStreamId.Registry.Should().Be(_fixture.RegistryOptions.RegistryUrls.First().Key);
        queriedCert.Quantity.Should().Be(cert.Quantity);
        queriedCert.Start.Should().Be(cert.Start);
        queriedCert.End.Should().Be(cert.End);
        queriedCert.GridArea.Should().Be(cert.GridArea);
        queriedCert.CertificateType.Should().Be(type.MapToWalletModel());
        queriedCert.Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn },
            { "fuelCode", "F01040100" },
            { "techCode", "T010000" }
        });
    }

    [Theory]
    [InlineData(CertificateType.Production)]
    [InlineData(CertificateType.Consumption)]
    public async Task IssueFiveCertificates(CertificateType type)
    {
        var walletClient = _poStack.CreateWalletClient(Guid.NewGuid().ToString());

        var endpointRef = await walletClient.CreateWalletAndEndpoint();

        var client = _fixture.CreateHttpClient();
        var recipientId = await client.AddRecipient(endpointRef);

        var gsrn = Some.Gsrn();
        const int certsCount = 5;
        var certs = Enumerable.Range(0, certsCount)
            .Select(i => new CertificateDto
            {
                Id = Guid.NewGuid(),
                Start = DateTimeOffset.UtcNow.AddHours(i).RoundToLatestHourLong(),
                End = DateTimeOffset.UtcNow.AddHours(i + 1).RoundToLatestHourLong(),
                GridArea = _fixture.RegistryOptions.IssuerPrivateKeyPems.First().Key,
                Quantity = (uint)(42 + i),
                Type = type,
                ClearTextAttributes = new Dictionary<string, string>
                {
                    { "fuelCode", "F01040100" },
                    { "techCode", "T010000" }
                },
                HashedAttributes = new List<HashedAttribute>
                {
                    new () { Key = "assetId", Value = gsrn }
                }
            })
        .ToArray();

        foreach (var cert in certs)
        {
            var response = await client.PostCertificate(recipientId, _fixture.RegistryOptions.RegistryUrls.First().Key, gsrn, cert);
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        var queryResponse = await walletClient.RepeatedlyGetCertificatesUntil(res => res.Count() == certsCount);

        certs = certs.OrderBy(gc => gc.Start).ToArray();
        var granularCertificates = queryResponse.OrderBy(gc => gc.Start).ToArray();

        for (int i = 0; i < certsCount; i++)
        {
            granularCertificates[i].FederatedStreamId.StreamId.Should().Be(certs[i].Id);
            granularCertificates[i].FederatedStreamId.Registry.Should().Be(_fixture.RegistryOptions.RegistryUrls.First().Key);
            granularCertificates[i].Start.Should().Be(certs[i].Start);
            granularCertificates[i].End.Should().Be(certs[i].End);
            granularCertificates[i].GridArea.Should().Be(certs[i].GridArea);
            granularCertificates[i].Quantity.Should().Be(certs[i].Quantity);
            granularCertificates[i].CertificateType.Should().Be(type.MapToWalletModel());
            granularCertificates[i].Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
            {
                { "assetId", gsrn },
                { "fuelCode", "F01040100" },
                { "techCode", "T010000" }
            });
        }
    }
}
