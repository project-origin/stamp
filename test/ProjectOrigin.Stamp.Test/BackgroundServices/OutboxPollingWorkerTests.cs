using System.Text.Json;
using MassTransit;
using NSubstitute;
using ProjectOrigin.Stamp.Database;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Stamp.BackgroundServices;
using ProjectOrigin.Stamp.Extensions;
using ProjectOrigin.Stamp.Models;
using ProjectOrigin.Stamp.Repositories;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.Stamp.EventHandlers;
using ProjectOrigin.Stamp.ValueObjects;

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
        var period = new Period(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds());
        var payloadObj = new CertificateStoredEvent
        {
            CertificateId = Guid.NewGuid(),
            CertificateType = GranularCertificateType.Production,
            Period = period,
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
            MessageType = typeof(CertificateStoredEvent).ToString(),
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

        // Act and ignore TaskCanceledException when Delay happens
        try
        {
            await sut.StartAsync(tokenSource.Token);
        }
        catch (TaskCanceledException)
        {
        }

        // Assert
        await busMock.DidNotReceive().Publish(Arg.Any<object?>(), Arg.Any<CancellationToken>());
        await outboxRepositoryMock.DidNotReceive().Delete(Arg.Any<Guid>());
        unitOfWorkMock.DidNotReceive().Commit();
        unitOfWorkMock.DidNotReceive().Rollback();
    }
}
