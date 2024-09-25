using System.Net;
using ProjectOrigin.Stamp.Server;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using FluentAssertions;
using ProjectOrigin.Stamp.Test.Extensions;
using Xunit;
using CertificateType = ProjectOrigin.Stamp.Server.Services.REST.v1.CertificateType;

namespace ProjectOrigin.Stamp.Test;

[Collection("EntireStackCollection")]
public class WithdrawCertificateControllerTests : IDisposable
{
    private readonly TestServerFixture<Startup> _fixture;
    private readonly string _gridArea;
    private readonly string _registryName;
    private readonly HttpClient _walletClient;
    private readonly HttpClient _client;
    private readonly string _gsrn;

    public WithdrawCertificateControllerTests(EntireStackFixture stack)
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
    public async Task WithdrawCertificate()
    {
        // Arrange
        var recipientId = await CreateRecipient();
        var cert = Some.CertificateDto(gsrn: _gsrn, gridArea: _gridArea);
        await _client.PostCertificate(recipientId, _registryName, _gsrn, cert);

        // Act
        var responseWithdraw = await _client.WithdrawCertificate(_registryName, cert.Id);

        // Assert
        responseWithdraw.StatusCode.Should().Be(HttpStatusCode.Created);
    }


    [Fact]
    public async Task WhenNoCertificateExistsInDatabase_NotFound()
    {
        // Arrange
        var cert = Some.CertificateDto(gsrn: _gsrn, gridArea: _gridArea);

        // Act
        var responseWithdraw = await _client.WithdrawCertificate(_registryName, cert.Id);

        // Assert
        responseWithdraw.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    [Fact]
    public async Task WhenCertificateAlreadyWithdrawn_Conflict()
    {
        // Arrange
        var recipientId = await CreateRecipient();
        var cert = Some.CertificateDto(gsrn: _gsrn, gridArea: _gridArea);
        await _client.PostCertificate(recipientId, _registryName, _gsrn, cert);

        // Act
        var response1 = await _client.WithdrawCertificate(_registryName, cert.Id);
        var response2 = await _client.WithdrawCertificate(_registryName, cert.Id);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
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
