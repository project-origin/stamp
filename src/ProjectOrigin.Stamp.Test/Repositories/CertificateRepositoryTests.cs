using FluentAssertions;
using Npgsql;
using ProjectOrigin.Stamp.Server.Repositories;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using ProjectOrigin.Stamp.Server.Models;
using Xunit;

namespace ProjectOrigin.Stamp.Test.Repositories;

public class CertificateRepositoryTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly CertificateRepository _repository;

    public CertificateRepositoryTests(PostgresDatabaseFixture dbFixture)
    {
        var connection = new NpgsqlConnection(dbFixture.ConnectionString);
        connection.Open();
        _repository = new CertificateRepository(connection);
    }

    [Fact]
    public async Task CreateAndGetCertificate()
    {
        var cert = new GranularCertificate
        {
            EndDate = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            GridArea = "DK1",
            ClearTextAttributes = new Dictionary<string, string>() { { "TechCode", "T12345" }  },
            HashedAttributes = new List<CertificateHashedAttribute>(),
            Id = Guid.NewGuid(),
            CertificateType = GranularCertificateType.Production,
            StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Quantity = 1234,
            RegistryName = "Energinet.dk"
        };

        cert.Reject("TestReason");

        await _repository.Create(cert);

        var queriedCert = await _repository.Get(cert.RegistryName, cert.Id);

        queriedCert.Should().NotBeNull();
        queriedCert!.ClearTextAttributes.Should().BeEquivalentTo(cert.ClearTextAttributes);
        queriedCert.GridArea.Should().Be(cert.GridArea);
        queriedCert.Id.Should().Be(cert.Id);
        queriedCert.Quantity.Should().Be(cert.Quantity);
        queriedCert.RegistryName.Should().Be(cert.RegistryName);
        queriedCert.StartDate.Should().Be(cert.StartDate);
        queriedCert.EndDate.Should().Be(cert.EndDate);
        queriedCert.CertificateType.Should().Be(cert.CertificateType);
        queriedCert.IssuedState.Should().Be(cert.IssuedState);
        queriedCert.RejectionReason.Should().Be(cert.RejectionReason);
    }

    [Fact]
    public async Task SetState()
    {
        var cert = new GranularCertificate
        {
            EndDate = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            GridArea = "DK1",
            ClearTextAttributes = new Dictionary<string, string>() { { "TechCode", "T12345" }  },
            HashedAttributes = new List<CertificateHashedAttribute>(),
            Id = Guid.NewGuid(),
            CertificateType = GranularCertificateType.Production,
            StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Quantity = 1234,
            RegistryName = "Energinet.dk"
        };

        await _repository.Create(cert);

        cert.Issue();

        await _repository.SetState(cert.Id, cert.RegistryName, cert.IssuedState);

        var queriedCert = await _repository.Get(cert.RegistryName, cert.Id);

        queriedCert.Should().NotBeNull();
        queriedCert!.IssuedState.Should().Be(cert.IssuedState);
    }

}
