using System.Diagnostics.Metrics;
using System.Net;
using ProjectOrigin.Stamp.Server;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using ProjectOrigin.Stamp.Test.Extensions;
using Xunit;
using CertificateType = ProjectOrigin.Stamp.Server.Services.REST.v1.CertificateType;

namespace ProjectOrigin.Stamp.Test;

[Collection("EntireStackCollection")]
public class CertificateIssuingTests : IDisposable
{
    private readonly TestServerFixture<Startup> _fixture;
    private readonly string _gridArea;
    private readonly string _registryName;
    private readonly HttpClient _walletClient;
    private readonly HttpClient _client;
    private readonly string _gsrn;

    public CertificateIssuingTests(EntireStackFixture stack)
    {
        _fixture = stack.testServer;
        _fixture.PostgresConnectionString = stack.postgres.ConnectionString;
        _fixture.RabbitMqOptions = stack.rabbitMq.Options;
        _fixture.RegistryOptions = stack.poStack.RegistryOptions;

        _gridArea = _fixture.RegistryOptions.IssuerPrivateKeyPems.First().Key;
        _registryName = _fixture.RegistryOptions.Registries[0].Name;
        _walletClient = stack.poStack.CreateWalletClient(Guid.NewGuid().ToString());
        _client = _fixture.CreateHttpClient();
        _gsrn = Some.Gsrn();
    }

    [Fact]
    public async Task NoCertificates()
    {
        var certs = await _walletClient.QueryCertificates();

        certs.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenEndDateIsBeforeStartDate_BadRequest()
    {
        var recipientId = await CreateRecipient();
        var now = DateTimeOffset.UtcNow;

        var cert = Some.CertificateDto(start: now.AddHours(1), end: now, gsrn: _gsrn);

        var response = await _client.PostCertificate(recipientId, _registryName, _gsrn, cert);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CanIssueAgainAfterWithdrawn()
    {
        var recipientId = await CreateRecipient();
        var now = DateTimeOffset.UtcNow;

        var cert1 = Some.CertificateDto(start: now.AddHours(-1), end: now, gsrn: _gsrn);

        var issueResponse1 = await _client.PostCertificate(recipientId, _registryName, _gsrn, cert1);
        issueResponse1.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var certs1 = await _walletClient.RepeatedlyGetCertificatesUntil(certs => certs.Any());

        var withdrawnResponse = await _client.WithdrawCertificate(_registryName, cert1.Id);

        await Task.Delay(TimeSpan.FromSeconds(30));

        var cert2 = Some.CertificateDto(start: now.AddHours(-1), end: now, gsrn: _gsrn);
        var issueResponse2 = await _client.PostCertificate(recipientId, _registryName, _gsrn, cert2);

        issueResponse2.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var certs2 = await _walletClient.RepeatedlyGetCertificatesUntil(certs => certs.Count() == 2);
        certs2.Select(x => x.FederatedStreamId.StreamId).Should().Contain(cert2.Id);
    }

    [Fact]
    public async Task CannotIssueCertificateWithSameCertificateIdAndRegistryNameAgainAfterWithdrawn()
    {
        var recipientId = await CreateRecipient();
        var now = DateTimeOffset.UtcNow;

        var cert1 = Some.CertificateDto(start: now.AddHours(-1), end: now, gsrn: _gsrn);

        var issueResponse1 = await _client.PostCertificate(recipientId, _registryName, _gsrn, cert1);
        issueResponse1.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var certs = await _walletClient.RepeatedlyGetCertificatesUntil(certs => certs.Any(), TimeSpan.FromSeconds(60));

        var withdrawnResponse = await _client.WithdrawCertificate(_registryName, cert1.Id);

        var issueResponse2 = await _client.PostCertificate(recipientId, _registryName, _gsrn, cert1);

        issueResponse2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task WhenCertificateWithMeteringPointIdStartAndEndAlreadyExists_Conflict()
    {
        var recipientId = await CreateRecipient();
        var cert = Some.CertificateDto(gsrn: _gsrn);
        var cert2 = Some.CertificateDto(gsrn: _gsrn);

        var response = await _client.PostCertificate(recipientId, _registryName, _gsrn, cert);
        var response2 = await _client.PostCertificate(recipientId, _registryName, _gsrn, cert2);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task WhenCertificateAlreadyExists_Conflict()
    {
        var recipientId = await CreateRecipient();
        var cert = Some.CertificateDto(gsrn: _gsrn);

        var response1 = await _client.PostCertificate(recipientId, _registryName, _gsrn, cert);
        var response2 = await _client.PostCertificate(recipientId, _registryName, "1234", cert);

        response1.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task WhenNonExistingRecipient_NotFound()
    {
        var nonExistingRecipientId = Guid.NewGuid();
        var cert = Some.CertificateDto(gsrn: _gsrn);

        var response = await _client.PostCertificate(nonExistingRecipientId, _registryName, _gsrn, cert);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(CertificateType.Production)]
    [InlineData(CertificateType.Consumption)]
    public async Task IssueCertificate(CertificateType type)
    {
        // Arrange
        var recipientId = await CreateRecipient();
        var cert = Some.CertificateDto(gsrn: _gsrn, type: type, gridArea: _gridArea);

        // Act
        var response = await _client.PostCertificate(recipientId, _registryName, _gsrn, cert);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var certs = await _walletClient.RepeatedlyGetCertificatesUntil(certs => certs.Any());
        certs.Should().HaveCount(1);
        var queriedCert = certs[0];

        queriedCert.FederatedStreamId.StreamId.Should().Be(cert.Id);
        queriedCert.FederatedStreamId.Registry.Should().Be(_registryName);
        queriedCert.Quantity.Should().Be(cert.Quantity);
        queriedCert.Start.Should().Be(cert.Start);
        queriedCert.End.Should().Be(cert.End);
        queriedCert.GridArea.Should().Be(cert.GridArea);
        queriedCert.CertificateType.Should().Be(type.MapToWalletModel());
        cert.ClearTextAttributes.All(atr => queriedCert.Attributes.ContainsKey(atr.Key) && queriedCert.Attributes.ContainsValue(atr.Value)).Should().BeTrue();
        cert.HashedAttributes.All(atr => queriedCert.Attributes.ContainsKey(atr.Key) && queriedCert.Attributes.ContainsValue(atr.Value)).Should().BeTrue();
    }

    [Theory]
    [InlineData(CertificateType.Production)]
    [InlineData(CertificateType.Consumption)]
    public async Task IssueFiveCertificates(CertificateType type)
    {
        // Arrange
        var recipientId = await CreateRecipient();
        const int certsCount = 5;
        var now = DateTimeOffset.UtcNow;
        var certs = Enumerable.Range(0, certsCount)
            .Select(i => Some.CertificateDto(_gridArea, (uint)(42 + i), now.AddHours(i), now.AddHours(i + 1), _gsrn, type))
            .ToArray();

        // Act
        foreach (var cert in certs)
        {
            var response = await _client.PostCertificate(recipientId, _registryName, _gsrn, cert);
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        // Assert
        var queryResponse = await _walletClient.RepeatedlyGetCertificatesUntil(res => res.Count() == certsCount);

        certs = certs.OrderBy(gc => gc.Start).ToArray();
        var granularCertificates = queryResponse.OrderBy(gc => gc.Start).ToArray();

        for (int i = 0; i < certsCount; i++)
        {
            granularCertificates[i].FederatedStreamId.StreamId.Should().Be(certs[i].Id);
            granularCertificates[i].FederatedStreamId.Registry.Should().Be(_registryName);
            granularCertificates[i].Start.Should().Be(certs[i].Start);
            granularCertificates[i].End.Should().Be(certs[i].End);
            granularCertificates[i].GridArea.Should().Be(certs[i].GridArea);
            granularCertificates[i].Quantity.Should().Be(certs[i].Quantity);
            granularCertificates[i].CertificateType.Should().Be(type.MapToWalletModel());
            granularCertificates[i].Attributes.Should().NotBeNullOrEmpty();
            certs[i].ClearTextAttributes.All(atr => granularCertificates[i].Attributes.ContainsKey(atr.Key) && granularCertificates[i].Attributes.ContainsValue(atr.Value)).Should().BeTrue();
            certs[i].HashedAttributes.All(atr => granularCertificates[i].Attributes.ContainsKey(atr.Key) && granularCertificates[i].Attributes.ContainsValue(atr.Value)).Should().BeTrue();
        }
    }

    [Fact]
    public async Task IssueSingleCertificate_IncrementsIssuedAndIntentsCountersByOne()
    {
        var meterFactory = _fixture.GetRequiredService<IMeterFactory>();
        var issuedCollector = new MetricCollector<long>(meterFactory, "ProjectOrigin.Stamp", "po_stamp_certificate_issued_count");
        var intentsCollector = new MetricCollector<long>(meterFactory, "ProjectOrigin.Stamp", "po_stamp_certificate_intent_received_count");
        var recipientId = await CreateRecipient();
        var cert = Some.CertificateDto(
            gsrn: _gsrn,
            type: CertificateType.Production,
            gridArea: _gridArea);

        var response = await _client.PostCertificate(recipientId, _registryName, _gsrn, cert);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var certs = await _walletClient.RepeatedlyGetCertificatesUntil(certs => certs.Any(), TimeSpan.FromSeconds(60));
        certs.Should().NotBeEmpty();
        var issuedMeasurements = issuedCollector.GetMeasurementSnapshot();
        var intentsMeasurements = intentsCollector.GetMeasurementSnapshot();

        issuedMeasurements[0].Tags["CertificateType"].Should().Be("Production");
        Assert.Equal(1, issuedMeasurements.EvaluateAsCounter());
        Assert.Equal(1, intentsMeasurements.EvaluateAsCounter());
    }

    private async Task<Guid> CreateRecipient()
    {
        var endpointRef = await _walletClient.CreateWalletAndEndpoint();
        var client = _fixture.CreateHttpClient();
        return await client.AddRecipient(endpointRef);
    }

    public void Dispose()
    {
        _walletClient.Dispose();
        _client.Dispose();
    }
}
