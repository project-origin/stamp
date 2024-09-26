using FluentAssertions;
using Npgsql;
using ProjectOrigin.Stamp.Server.Repositories;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using Xunit;

namespace ProjectOrigin.Stamp.Test.Repositories;

public class WithdrawnCertificateRepositoryTests : IClassFixture<PostgresDatabaseFixture>
{
    private const int PAGE_SIZE = 3;
    private const int PAGE_NUMBER = 1;

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
        var registryName = "Narnia";

        // Act
        await _repository.Create(registryName, certificateId);

        // Assert
        var withdrawnCertificate = await _repository.Get(registryName, certificateId);
        withdrawnCertificate.Should().NotBeNull();
        withdrawnCertificate!.CertificateId.Should().Be(certificateId);
        withdrawnCertificate.RegistryName.Should().Be(registryName);
    }

    [Fact]
    public async Task GetPage_WhenPreviousIdIsUsed_Single()
    {
        // Arrange
        var withdrawnCertificate = await _repository.Create("Narnia", Guid.NewGuid());
        int fromId = withdrawnCertificate.Id - 1;

        // Act
        var page = await _repository.GetPage(fromId, PAGE_SIZE, PAGE_NUMBER);

        // Assert
        page.Should().ContainSingle();
    }

    [Fact]
    public async Task GetPage_WhenCurrentIdUsed_Empty()
    {
        // Arrange
        var withdrawnCertificate = await _repository.Create("Narnia", Guid.NewGuid());
        int fromId = withdrawnCertificate.Id;

        // Act
        var page = await _repository.GetPage(fromId, PAGE_SIZE, PAGE_NUMBER);

        // Assert
        page.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPage_WhenSecondPageIsQueried_Empty()
    {
        // Arrange
        var ids = await CreateWithdrawnCertificates(12);
        int fromId = ids[0] - 1;
        int pageNumber = 2;

        // Act
        var page = await _repository.GetPage(fromId, PAGE_SIZE, pageNumber);

        // Assert
        page.Count.Should().Be(PAGE_SIZE);
        page[0].Id.Should().Be(ids[3]);
        page[1].Id.Should().Be(ids[4]);
        page[2].Id.Should().Be(ids[5]);
    }

    [Fact]
    public async Task GetPage_WhenNoneFullPageIsQueried_2Entities()
    {
        // Arrange
        var ids = await CreateWithdrawnCertificates(5);
        int fromId = ids[0] - 1;
        int pageNumber = 2;

        // Act
        var page = await _repository.GetPage(fromId, PAGE_SIZE, pageNumber);

        // Assert
        page.Count.Should().Be(2);
        page[0].Id.Should().Be(ids[3]);
        page[1].Id.Should().Be(ids[4]);
    }

    [Fact]
    public async Task GetPage_WhenTooHighPageNumberIsQueried_Empty()
    {
        // Arrange
        int pageNumber = 3;
        var ids = await CreateWithdrawnCertificates(6);
        int fromId = ids[0] - 1;

        // Act
        var page = await _repository.GetPage(fromId, PAGE_SIZE, pageNumber);

        // Assert
        page.Should().BeEmpty();
    }

    private async Task<List<int>> CreateWithdrawnCertificates(int count)
    {
        var ids = new List<int>();
        for (int i = 0; i < count; i++)
        {
            var withdrawnCertificate = await _repository.Create("Narnia", Guid.NewGuid());
            ids.Add(withdrawnCertificate.Id);
        }
        return ids;
    }
}
