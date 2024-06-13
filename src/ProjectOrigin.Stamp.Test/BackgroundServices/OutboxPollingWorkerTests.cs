using System.Text.Json;
using MassTransit;
using NSubstitute;
using ProjectOrigin.Stamp.Server.Database;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Stamp.Server.BackgroundServices;
using ProjectOrigin.Stamp.Server.Extensions;
using ProjectOrigin.Stamp.Server.Models;
using ProjectOrigin.Stamp.Server.Repositories;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.Stamp.Server.EventHandlers;

namespace ProjectOrigin.Stamp.Test.BackgroundServices;

public class OutboxPollingWorkerTests
{
    private readonly IServiceScope scopeMock = Substitute.For<IServiceScope>();
    private readonly IServiceScopeFactory scopeFactoryMock = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceProvider serviceProviderMock = Substitute.For<IServiceProvider>();
    private readonly IUnitOfWork unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IBus busMock = Substitute.For<IBus>();
    private readonly ILogger<OutboxPollingWorker> loggerMock = Substitute.For<ILogger<OutboxPollingWorker>>();
    private readonly IOutboxMessageRepository outboxRepositoryMock = Substitute.For<IOutboxMessageRepository>();
    private readonly OutboxPollingWorker sut;

    public OutboxPollingWorkerTests()
    {
        serviceProviderMock.GetService<IUnitOfWork>().Returns(unitOfWorkMock);
        serviceProviderMock.GetService<IBus>().Returns(busMock);
        scopeMock.ServiceProvider.Returns(serviceProviderMock);
        scopeFactoryMock.CreateScope().Returns(scopeMock);
        serviceProviderMock.GetService<IServiceScopeFactory>().Returns(scopeFactoryMock);
        serviceProviderMock.CreateScope().Returns(scopeMock);

        sut = new OutboxPollingWorker(serviceProviderMock, loggerMock);
    }

    [Fact]
    public async Task ShouldPublishAndDeleteMessages()
    {
        var privateKey = new Secp256k1Algorithm().GenerateNewPrivateKey();
        var payloadObj = new CertificateCreatedEvent
        {
            CertificateId = Guid.NewGuid(),
            CertificateType = GranularCertificateType.Production,
            Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            GridArea = "DK1",
            ClearTextAttributes = new Dictionary<string, string> { { "TechCode", "T12345" } },
            HashedAttributes = new List<CertificateHashedAttribute>(),
            Quantity = 1234,
            RegistryName = "Energinet.dk",
            WalletEndpointReferencePublicKey = privateKey.Neuter().Export().ToArray(),
            RecipientId = Guid.NewGuid()
        };
        var message = new OutboxMessage
        {
            Created = DateTimeOffset.Now.ToUtcTime(),
            JsonPayload = JsonSerializer.Serialize(payloadObj),
            MessageType = typeof(CertificateCreatedEvent).ToString(),
            Id = Guid.NewGuid()
        };
        using var tokenSource = new CancellationTokenSource();
        outboxRepositoryMock.GetFirst().Returns(message);
        unitOfWorkMock.OutboxMessageRepository.Returns(outboxRepositoryMock);
        busMock
            .Publish(Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(Task.CompletedTask)
            .AndDoes(_ => tokenSource.Cancel());

        // Act
        await sut.StartAsync(tokenSource.Token);

        // Assert
        await busMock.Received(1).Publish(Arg.Any<object?>(), Arg.Any<CancellationToken>());
        await outboxRepositoryMock.Received(1).Delete(message.Id);
        unitOfWorkMock.Received(1).Commit();
        unitOfWorkMock.DidNotReceive().Rollback();
    }

    [Fact]
    public async Task WhenMessageIsNull_ShouldNotPublishAndDelete()
    {
        using var tokenSource = new CancellationTokenSource();
        outboxRepositoryMock.GetFirst()
            .Returns((OutboxMessage)null)
            .AndDoes(_ => tokenSource.Cancel());
        unitOfWorkMock.OutboxMessageRepository.Returns(outboxRepositoryMock);

        // Act
        await sut.StartAsync(tokenSource.Token);

        // Assert
        await busMock.DidNotReceive().Publish(Arg.Any<object?>(), Arg.Any<CancellationToken>());
        await outboxRepositoryMock.DidNotReceive().Delete(Arg.Any<Guid>());
        unitOfWorkMock.DidNotReceive().Commit();
        unitOfWorkMock.DidNotReceive().Rollback();
    }
}
