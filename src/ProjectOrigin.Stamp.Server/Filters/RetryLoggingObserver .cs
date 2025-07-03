using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ProjectOrigin.Stamp.Server.Filters;

public class RetryLoggingObserver(ILogger<RetryLoggingObserver> logger) : IRetryObserver
{
    private static readonly ConcurrentDictionary<Guid, DateTime> StartTimes = new();

    private static Guid? GetMessageId<T>(T context) where T : class, PipeContext
    {
        if (context is ConsumeContext consumeContext)
        {
            return consumeContext.MessageId;
        }
        return null;
    }

    public Task PostCreate<T>(RetryPolicyContext<T> context) where T : class, PipeContext
    {
        var certId = GetMessageId(context.Context);
        if (certId.HasValue)
            StartTimes[certId.Value] = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task PreRetry<T>(RetryContext<T> context) where T : class, PipeContext
    {
        var certId = GetMessageId(context.Context);
        if (certId.HasValue && StartTimes.TryGetValue(certId.Value, out var start))
        {
            var elapsed = DateTime.UtcNow - start;
            logger.LogInformation("Retrying CertificateId {CertificateId} after {ElapsedSeconds} seconds", certId, elapsed.TotalSeconds);
        }
        return Task.CompletedTask;
    }

    public Task PostFault<T>(RetryContext<T> context) where T : class, PipeContext => Task.CompletedTask;
    public Task RetryFault<T>(RetryContext<T> context) where T : class, PipeContext => Task.CompletedTask;

    public Task RetryComplete<T>(RetryContext<T> context) where T : class, PipeContext
    {
        var certId = GetMessageId(context.Context);
        if (certId.HasValue && StartTimes.TryRemove(certId.Value, out var start))
        {
            var elapsed = DateTime.UtcNow - start;
            logger.LogInformation(context.Exception, "All retries exhausted for CertificateId {CertificateId} after {ElapsedSeconds} seconds", certId, elapsed.TotalSeconds);
        }
        else
        {
            logger.LogInformation(context.Exception, "All retries exhausted for message {MessageType}", typeof(T).Name);
        }
        return Task.CompletedTask;
    }
}

