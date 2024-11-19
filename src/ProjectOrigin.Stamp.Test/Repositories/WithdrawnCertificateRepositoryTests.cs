using Dapper;
using FluentAssertions;
using Npgsql;
using ProjectOrigin.Stamp.Server.Repositories;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using Xunit;

namespace ProjectOrigin.Stamp.Test.Repositories;

public class WithdrawnCertificateRepositoryTests : IClassFixture<PostgresDatabaseFixture>
{
    private const int SKIP = 0;
    private const int LIMIT = 3;

    private readonly CertificateRepository _certificateRepository;
    private readonly WithdrawnCertificateRepository _repository;
    private readonly NpgsqlConnection _connection;

    public WithdrawnCertificateRepositoryTests(PostgresDatabaseFixture dbFixture)
    {
        _connection = new NpgsqlConnection(dbFixture.ConnectionString);
        _connection.Open();
        _repository = new WithdrawnCertificateRepository(_connection);
        _certificateRepository = new CertificateRepository(_connection);
    }

    [Fact]
    public async Task CreateAndGetWithdrawnCertificate()
    {
        var certificate = Some.GranularCertificate();

        await _certificateRepository.Create(certificate);
        await _repository.Withdraw(certificate);

        var withdrawnCertificate = await _repository.Get(certificate.RegistryName, certificate.Id);
        withdrawnCertificate.Should().NotBeNull();
        withdrawnCertificate!.CertificateId.Should().Be(certificate.Id);
        withdrawnCertificate.RegistryName.Should().Be(certificate.RegistryName);
        withdrawnCertificate.CertificateType.Should().Be(certificate.CertificateType);
        withdrawnCertificate.Quantity.Should().Be(certificate.Quantity);
        withdrawnCertificate.StartDate.Should().Be(certificate.StartDate);
        withdrawnCertificate.EndDate.Should().Be(certificate.EndDate);
        withdrawnCertificate.GridArea.Should().Be(certificate.GridArea);
        withdrawnCertificate.IssuedState.Should().Be(certificate.IssuedState);
        withdrawnCertificate.RejectionReason.Should().Be(certificate.RejectionReason);
        withdrawnCertificate.MeteringPointId.Should().Be(certificate.MeteringPointId);
        withdrawnCertificate.ClearTextAttributes.Should().BeEquivalentTo(certificate.ClearTextAttributes);
        withdrawnCertificate.HashedAttributes.Should().BeEquivalentTo(certificate.HashedAttributes);
    }

    [Fact]
    public async Task GetMultiple_WhenPreviousIdIsUsed_Single()
    {
        // Arrange
        var certificate = Some.GranularCertificate();
        await _certificateRepository.Create(certificate);
        var withdrawnCertificate = await _repository.Withdraw(certificate);
        int fromId = withdrawnCertificate.Id - 1;

        // Act
        var page = await _repository.GetMultiple(fromId, SKIP, LIMIT);

        // Assert
        page.Count.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.TotalCount.Should().Be(1);
        page.Limit.Should().Be(LIMIT);
        page.Offset.Should().Be(SKIP);
    }

    [Fact]
    public async Task GetMultiple_WhenCurrentIdUsed_Empty()
    {
        // Arrange
        var certificate = Some.GranularCertificate();
        await _certificateRepository.Create(certificate);
        var withdrawnCertificate = await _repository.Withdraw(certificate);
        int fromId = withdrawnCertificate.Id;

        // Act
        var page = await _repository.GetMultiple(fromId, SKIP, LIMIT);

        // Assert
        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
        page.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetMultiple_WhenSkippingAll_Empty()
    {
        // Arrange
        var certificatesCount = 3;
        var ids = await CreateWithdrawnCertificates(certificatesCount);
        int fromId = ids[0] - 1;

        // Act
        var page = await _repository.GetMultiple(fromId, certificatesCount, int.MaxValue);

        // Assert
        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(certificatesCount);
        page.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetMultiple_WhenSkippingOne_ExpectRest()
    {
        // Arrange
        var certificatesCount = 3;
        var ids = await CreateWithdrawnCertificates(certificatesCount);
        int fromId = ids[0] - 1;

        // Act
        var page = await _repository.GetMultiple(fromId, certificatesCount - 1, int.MaxValue);

        // Assert
        page.Items.Should().HaveCount(1);
        page.TotalCount.Should().Be(certificatesCount);
        page.Count.Should().Be(1);
        page.Offset.Should().Be(certificatesCount - 1);
        page.Limit.Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task GetMultiple_WhenLimiting_ExpectLimit()
    {
        // Arrange
        var certificatesCount = 3;
        var ids = await CreateWithdrawnCertificates(certificatesCount);
        int fromId = ids[0] - 1;

        // Act
        var page = await _repository.GetMultiple(fromId, 0, certificatesCount - 1);

        // Assert
        page.Items.Should().HaveCount(2);
        page.TotalCount.Should().Be(certificatesCount);
        page.Count.Should().Be(2);
        page.Offset.Should().Be(0);
        page.Limit.Should().Be(certificatesCount - 1);
    }

    [Fact]
    public async Task GetMultiple_WhenSkippingAndLimiting()
    {
        // Arrange
        await TruncateWithdrawnCertificates();
        var limit = 2;
        var skip = 1;
        var certificatesCount = 6;
        var ids = await CreateWithdrawnCertificates(certificatesCount);

        // Act
        var page = await _repository.GetMultiple(0, skip, limit);

        // Assert
        page.Items.Should().HaveCount(limit);
        page.TotalCount.Should().Be(certificatesCount);
        page.Count.Should().Be(limit);
        page.Offset.Should().Be(skip);
        page.Limit.Should().Be(limit);
        page.Items.ToArray()[0].Id.Should().Be(ids[1]);
        page.Items.ToArray()[1].Id.Should().Be(ids[2]);
    }

    private async Task TruncateWithdrawnCertificates()
    {
        await _connection.ExecuteAsync("TRUNCATE TABLE WithdrawnCertificates");
    }

    private async Task<List<int>> CreateWithdrawnCertificates(int count)
    {
        var ids = new List<int>();
        for (int i = 0; i < count; i++)
        {
            var certificate = Some.GranularCertificate();
            await _certificateRepository.Create(certificate);
            var withdrawnCertificate = await _repository.Withdraw(certificate);
            ids.Add(withdrawnCertificate.Id);
        }
        return ids;
    }
}
