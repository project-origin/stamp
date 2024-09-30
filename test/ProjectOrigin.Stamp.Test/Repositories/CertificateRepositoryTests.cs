using FluentAssertions;
using Npgsql;
using ProjectOrigin.Stamp.Repositories;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using ProjectOrigin.Stamp.Models;
using ProjectOrigin.Stamp.ValueObjects;
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
            ClearTextAttributes = new Dictionary<string, string>()
            {
                { "TechCode", "T12345" },
                { "FuelCode", "T123456" }
            },
            HashedAttributes =
            [
                new() { HaKey = "AssetId", HaValue = "1234", Salt = Guid.NewGuid().ToByteArray() }
            ],
            Id = Guid.NewGuid(),
            CertificateType = GranularCertificateType.Production,
            StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Quantity = 1234,
            RegistryName = "Energinet.dk",
            MeteringPointId = Some.Gsrn()
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
        queriedCert.HashedAttributes.Should().BeEquivalentTo(cert.HashedAttributes);
    }

    [Fact]
    public async Task SetState()
    {
        var cert = new GranularCertificate
        {
            EndDate = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            GridArea = "DK1",
            ClearTextAttributes = new Dictionary<string, string>() { { "TechCode", "T12345" } },
            HashedAttributes = new List<CertificateHashedAttribute>(),
            Id = Guid.NewGuid(),
            CertificateType = GranularCertificateType.Production,
            StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Quantity = 1234,
            RegistryName = "Energinet.dk",
            MeteringPointId = Some.Gsrn()
        };

        await _repository.Create(cert);

        cert.Issue();

        await _repository.SetState(cert.Id, cert.RegistryName, cert.IssuedState);

        var queriedCert = await _repository.Get(cert.RegistryName, cert.Id);

        queriedCert.Should().NotBeNull();
        queriedCert!.IssuedState.Should().Be(cert.IssuedState);
    }

    [Fact]
    public async Task Reject()
    {
        var cert = new GranularCertificate
        {
            EndDate = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            GridArea = "DK1",
            ClearTextAttributes = new Dictionary<string, string>() { { "TechCode", "T12345" } },
            HashedAttributes = new List<CertificateHashedAttribute>(),
            Id = Guid.NewGuid(),
            CertificateType = GranularCertificateType.Production,
            StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Quantity = 1234,
            RegistryName = "Energinet.dk",
            MeteringPointId = Some.Gsrn()
        };

        await _repository.Create(cert);

        cert.Reject("TestReason");

        await _repository.SetState(cert.Id, cert.RegistryName, cert.IssuedState, cert.RejectionReason);

        var queriedCert = await _repository.Get(cert.RegistryName, cert.Id);

        queriedCert.Should().NotBeNull();
        queriedCert!.IssuedState.Should().Be(cert.IssuedState);
        queriedCert.RejectionReason.Should().Be(cert.RejectionReason);
    }

    [Fact]
    public async Task Create_WhenSameMeteringPointIdAndPeriod_ExpectException()
    {
        var cert = new GranularCertificate
        {
            EndDate = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            GridArea = "DK1",
            ClearTextAttributes = new Dictionary<string, string>() { { "TechCode", "T12345" } },
            HashedAttributes = new List<CertificateHashedAttribute>(),
            Id = Guid.NewGuid(),
            CertificateType = GranularCertificateType.Production,
            StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Quantity = 1234,
            RegistryName = "Energinet.dk",
            MeteringPointId = Some.Gsrn()
        };

        await _repository.Create(cert);

        var cert2 = new GranularCertificate
        {
            EndDate = cert.EndDate,
            GridArea = "DK1",
            ClearTextAttributes = new Dictionary<string, string>() { { "TechCode", "T12345" } },
            HashedAttributes = new List<CertificateHashedAttribute>(),
            Id = Guid.NewGuid(),
            CertificateType = GranularCertificateType.Production,
            StartDate = cert.StartDate,
            Quantity = 1234,
            RegistryName = "Energinet.dk",
            MeteringPointId = cert.MeteringPointId
        };

        await _repository.Invoking(r => r.Create(cert2)).Should().ThrowAsync<PostgresException>();
    }

    [Fact]
    public async Task CertificateExists()
    {
        var cert = new GranularCertificate
        {
            EndDate = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            GridArea = "DK1",
            ClearTextAttributes = new Dictionary<string, string>() { { "TechCode", "T12345" } },
            HashedAttributes = new List<CertificateHashedAttribute>(),
            Id = Guid.NewGuid(),
            CertificateType = GranularCertificateType.Production,
            StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Quantity = 1234,
            RegistryName = "Energinet.dk",
            MeteringPointId = Some.Gsrn()
        };

        await _repository.Create(cert);

        var result = await _repository.CertificateExists(cert.MeteringPointId, new Period(cert.StartDate, cert.EndDate));

        result.Should().BeTrue();
    }
}
