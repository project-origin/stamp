using FluentAssertions;
using Npgsql;
using ProjectOrigin.Stamp.Server.Repositories;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using Xunit;

namespace ProjectOrigin.Stamp.Test.Repositories;

public class WithdrawnCertificateRepositoryTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly WithdrawnCertificateRepository _repository;

    public WithdrawnCertificateRepositoryTests(PostgresDatabaseFixture dbFixture)
    {
        var connection = new NpgsqlConnection(dbFixture.ConnectionString);
        connection.Open();
        _repository = new WithdrawnCertificateRepository(connection);
    }

    [Fact]
    public async Task CreateAndGetCertificate()
    {
        // Arrange
        var certificateId = Guid.NewGuid();
        var registryName = "Energinet.dk";

        // Act
        await _repository.Create(registryName, certificateId);

        // Assert
        var withdrawnCertificate = await _repository.Get(registryName, certificateId);
        withdrawnCertificate.Should().NotBeNull();
        withdrawnCertificate!.CertificateId.Should().Be(certificateId);
        withdrawnCertificate.RegistryName.Should().Be(registryName);
    }
}
