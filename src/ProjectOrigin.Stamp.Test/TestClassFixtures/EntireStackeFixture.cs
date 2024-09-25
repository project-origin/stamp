using ProjectOrigin.Stamp.Server;
using Xunit;

namespace ProjectOrigin.Stamp.Test.TestClassFixtures;

public class EntireStackFixture : IAsyncLifetime
{
    public PostgresDatabaseFixture postgres;
    public ProjectOriginStack poStack;
    public RabbitMqContainer rabbitMq;
    public TestServerFixture<Startup> testServer;

    public EntireStackFixture()
    {
        postgres = new PostgresDatabaseFixture();
        poStack = new ProjectOriginStack();
        rabbitMq = new RabbitMqContainer();
        testServer = new TestServerFixture<Startup>();
    }

    public async Task InitializeAsync()
    {
        await postgres.InitializeAsync();
        await poStack.InitializeAsync();
        await rabbitMq.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await poStack.DisposeAsync();
        await postgres.DisposeAsync();
        await rabbitMq.DisposeAsync();
    }
}
