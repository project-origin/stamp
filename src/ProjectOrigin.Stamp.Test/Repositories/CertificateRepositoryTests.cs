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
            End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            GridArea = "DK1",
            ClearTextAttributes = new Dictionary<string, string>() { { "TechCode", "T12345" }  },
            HashedAttributes = new List<CertificateHashedAttribute>(),
            Id = Guid.NewGuid(),
            Type = GranularCertificateType.Production,
            Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Quantity = 1234,
            RegistryName = "Energinet.dk"
        };

        await _repository.Create(cert);

        var queriedCert = await _repository.Get(cert.RegistryName, cert.Id);

        queriedCert.Should().NotBeNull();
        queriedCert!.ClearTextAttributes.Should().BeEquivalentTo(cert.ClearTextAttributes);
        queriedCert.GridArea.Should().Be(cert.GridArea);
        queriedCert.Id.Should().Be(cert.Id);
        queriedCert.Quantity.Should().Be(cert.Quantity);
        queriedCert.RegistryName.Should().Be(cert.RegistryName);
        queriedCert.Start.Should().Be(cert.Start);
        queriedCert.End.Should().Be(cert.End);
        queriedCert.Type.Should().Be(cert.Type);
    }
}
