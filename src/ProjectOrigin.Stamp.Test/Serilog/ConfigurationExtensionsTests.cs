using Xunit;
using Microsoft.Extensions.Configuration;
using ProjectOrigin.Stamp.Server.Extensions;
using Serilog;

namespace ProjectOrigin.Stamp.Test.Serilog;

public class Test
{
    [Fact]
    public void LogOutput_WithMassTransitRRetryInStructuredLogging_ExcludesLogs()
    {
        // Arrange
        var inMemorySettings = new List<KeyValuePair<string, string?>>
        {
            new("LogOutputFormat", "json")
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var output = new StringWriter();
        Console.SetOut(new StringWriter());

        var logger = configuration.GetSeriLogger();

        // Act
        logger
            .ForContext("SourceContext", "MassTransit.ReceiveTransport")
            .ForContext("MessageTemplate", "R-RETRY {InputAddress} {MessageId} {MessageType}")
            .ForContext("TraceId", "0f42ca4998cdada788181186db329640")
            .ForContext("SpanId", "b5346b195559e7cd")
            .ForContext("Exception", "ProjectOrigin.Stamp.Server.EventHandlers.RegistryTransactionStillProcessingException: Transaction wAlcfUeOfWF7iUO3pmKXi+djR+zYATURjo69utxFt6Q= is still processing on registry for certificateId: 6a1d25a9-6787-494f-a588-9fb7f6bac37c.\n   at ProjectOrigin.Stamp.Server.EventHandlers.WaitForCommittedRegistryTransactionConsumer.Consume(ConsumeContext`1 context) in /src/ProjectOrigin.Stamp.Server/EventHandlers/WaitForCommittedRegistryTransactionConsumer.cs:line 76\n   at MassTransit.DependencyInjection.ScopeConsumerFactory`1.Send[TMessage](ConsumeContext`1 context, IPipe`1 next) in /_/src/MassTransit/DependencyInjection/DependencyInjection/ScopeConsumerFactory.cs:line 22\n   at MassTransit.DependencyInjection.ScopeConsumerFactory`1.Send[TMessage](ConsumeContext`1 context, IPipe`1 next) in /_/src/MassTransit/DependencyInjection/DependencyInjection/ScopeConsumerFactory.cs:line 22\n   at MassTransit.Middleware.ConsumerMessageFilter`2.MassTransit.IFilter<MassTransit.ConsumeContext<TMessage>>.Send(ConsumeContext`1 context, IPipe`1 next) in /_/src/MassTransit/Middleware/ConsumerMessageFilter.cs:line 48\n   at MassTransit.Middleware.ConsumerMessageFilter`2.MassTransit.IFilter<MassTransit.ConsumeContext<TMessage>>.Send(ConsumeContext`1 context, IPipe`1 next) in /_/src/MassTransit/Middleware/ConsumerMessageFilter.cs:line 73\n   at MassTransit.Middleware.RetryFilter`1.MassTransit.IFilter<TContext>.Send(TContext context, IPipe`1 next) in /_/src/MassTransit/Middleware/RetryFilter.cs:line 47")
            .ForContext("InputAddress", "rabbitmq://rabbitmq/wait-for-committed-registry-transaction")
            .ForContext("MessageId", "01000000-b436-1ef5-45cb-08ddbad6627f")
            .ForContext("MessageType", "MassTransit.RetryPolicies.RetryConsumeContext<ProjectOrigin.Stamp.Server.EventHandlers.CertificateSentToRegistryEvent>")
            .ForContext("ParentId", "2235a58033c47f60")
            .Warning("R-RETRY {InputAddress} {MessageId} {MessageType}",
                "rabbitmq://rabbitmq/wait-for-committed-registry-transaction",
                "01000000-b436-1ef5-45cb-08ddbad6627f",
                "MassTransit.RetryPolicies.RetryConsumeContext<ProjectOrigin.Stamp.Server.EventHandlers.CertificateSentToRegistryEvent>");

        Log.CloseAndFlush();
        var logOutput = output.ToString();

        // Assert
        Assert.DoesNotContain("R-RETRY", logOutput);
    }

    [Fact]
    public void LogOutput_WithMassTransitRRetryInJsonLogging_ExcludesLogs()
    {
        // Arrange
        var inMemorySettings = new List<KeyValuePair<string, string?>>
        {
            new("LogOutputFormat", "json")
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var output = new StringWriter();
        Console.SetOut(output);

        var logger = configuration.GetSeriLogger();

        var jsonString = @"{""Timestamp"":""2025-07-04T08:40:09.5774548+00:00"",""Level"":""Warning"",""MessageTemplate"":""R-RETRY {InputAddress} {MessageId} {MessageType}"",""TraceId"":""0f42ca4998cdada788181186db329640"",""SpanId"":""b5346b195559e7cd"",""Exception"":""ProjectOrigin.Stamp.Server.EventHandlers.RegistryTransactionStillProcessingException: Transaction wAlcfUeOfWF7iUO3pmKXi+djR+zYATURjo69utxFt6Q= is still processing on registry for certificateId: 6a1d25a9-6787-494f-a588-9fb7f6bac37c.\n   at ProjectOrigin.Stamp.Server.EventHandlers.WaitForCommittedRegistryTransactionConsumer.Consume(ConsumeContext`1 context) in /src/ProjectOrigin.Stamp.Server/EventHandlers/WaitForCommittedRegistryTransactionConsumer.cs:line 76\n   at MassTransit.DependencyInjection.ScopeConsumerFactory`1.Send[TMessage](ConsumeContext`1 context, IPipe`1 next) in /_/src/MassTransit/DependencyInjection/DependencyInjection/ScopeConsumerFactory.cs:line 22\n   at MassTransit.DependencyInjection.ScopeConsumerFactory`1.Send[TMessage](ConsumeContext`1 context, IPipe`1 next) in /_/src/MassTransit/DependencyInjection/DependencyInjection/ScopeConsumerFactory.cs:line 22\n   at MassTransit.Middleware.ConsumerMessageFilter`2.MassTransit.IFilter<MassTransit.ConsumeContext<TMessage>>.Send(ConsumeContext`1 context, IPipe`1 next) in /_/src/MassTransit/Middleware/ConsumerMessageFilter.cs:line 48\n   at MassTransit.Middleware.ConsumerMessageFilter`2.MassTransit.IFilter<MassTransit.ConsumeContext<TMessage>>.Send(ConsumeContext`1 context, IPipe`1 next) in /_/src/MassTransit/Middleware/ConsumerMessageFilter.cs:line 73\n   at MassTransit.Middleware.RetryFilter`1.MassTransit.IFilter<TContext>.Send(TContext context, IPipe`1 next) in /_/src/MassTransit/Middleware/RetryFilter.cs:line 47"",""Properties"":{""InputAddress"":""rabbitmq://rabbitmq/wait-for-committed-registry-transaction"",""MessageId"":""01000000-b436-1ef5-45cb-08ddbad6627f"",""MessageType"":""MassTransit.RetryPolicies.RetryConsumeContext<ProjectOrigin.Stamp.Server.EventHandlers.CertificateSentToRegistryEvent>"",""SourceContext"":""MassTransit.ReceiveTransport"",""SpanId"":""b5346b195559e7cd"",""TraceId"":""0f42ca4998cdada788181186db329640"",""ParentId"":""2235a58033c47f60""}}";

        // Act
        logger.Warning(jsonString);

        Log.CloseAndFlush();
        var logOutput = output.ToString();

        // Assert
        Assert.DoesNotContain("R-RETRY", logOutput);
    }
}
