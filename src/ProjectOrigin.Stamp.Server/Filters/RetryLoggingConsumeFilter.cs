using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.RetryPolicies;
using Microsoft.Extensions.Logging;

namespace ProjectOrigin.Stamp.Server.Filters;

public class RetryLoggingConsumeFilter<T>(ILogger<RetryLoggingConsumeFilter<T>> logger) : IFilter<ConsumeContext<T>> where T : class
{
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("retryLoggingConsumeFilter");
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        try
        {
            await next.Send(context);
        }
        catch (Exception)
        {
            if (context is RetryConsumeContext retryContext)
            {
                var count = retryContext.RetryCount;
                var attempt = retryContext.RetryAttempt;

                logger.LogInformation("RetryCount {Count}, Attempt {Attempt}", count, attempt);
            }
            throw;
        }
    }
}
