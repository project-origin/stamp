using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ProjectOrigin.Stamp.Server.Filters;

public class RetryLoggingObserver(ILogger<RetryLoggingObserver> logger) : IRetryObserver
{
    public Task PostCreate<T>(RetryPolicyContext<T> context) where T : class, PipeContext
    {
        return Task.CompletedTask;
    }

    public Task PreRetry<T>(RetryContext<T> context) where T : class, PipeContext
    {
        return Task.CompletedTask;
    }

    public Task PostFault<T>(RetryContext<T> context) where T : class, PipeContext
    {
        return Task.CompletedTask;
    }

    public Task RetryFault<T>(RetryContext<T> context) where T : class, PipeContext
    {
        return Task.CompletedTask;
    }

    public Task RetryComplete<T>(RetryContext<T> context) where T : class, PipeContext
    {
        logger.LogWarning(context.Exception, "All retries exhausted for message {MessageType}", typeof(T).Name);
        return Task.CompletedTask;
    }
}

