using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using ProjectOrigin.Stamp.Server.Options;
using Testcontainers.RabbitMq;
using Xunit;

namespace ProjectOrigin.Stamp.Test.TestClassFixtures;

public partial class RabbitMqContainer : IAsyncLifetime
{
    private readonly global::Testcontainers.RabbitMq.RabbitMqContainer testContainer;

    [GeneratedRegex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:(\d+)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IpAndPortRegex();

    private readonly IFutureDockerImage rabbitMqImage;

    public RabbitMqContainer()
    {
        rabbitMqImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetProjectDirectory(), string.Empty)
            .WithDockerfile("rabbitmq.dockerfile")
            .WithName("rabbitmq_test")
            .Build();

        testContainer = new RabbitMqBuilder()
            .WithImage(rabbitMqImage)
            .WithPortBinding(12345, 15672)
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

    }
    public RabbitMqOptions Options
    {
        get
        {
            var match = IpAndPortRegex().Match(testContainer.GetConnectionString());

            return new RabbitMqOptions
            {
                Host = testContainer.Hostname,
                Port = ushort.Parse(match.Groups[1].Value),
                Username = "guest",
                Password = "guest"
            };
        }
    }

    public async Task InitializeAsync()
    {
        await rabbitMqImage.CreateAsync();
        await testContainer.StartAsync();
    }

    public async Task StopAsync() => await testContainer.StopAsync();

    public Task DisposeAsync() => testContainer.DisposeAsync().AsTask();
}
