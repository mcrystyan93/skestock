using System.Diagnostics;
using skestock.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace skestock.Application.Common.Behaviours;

public class PerformanceBehaviour<TRequest, TResponse>(
    ILogger<TRequest> logger,
    IUser user)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    private const long LongRunningThresholdMilliseconds = 500;

    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        // Measured per invocation: a scoped behaviour instance can serve several sends.
        var startedAt = Stopwatch.GetTimestamp();

        var response = await next(request, cancellationToken);

        var elapsedMilliseconds = (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (elapsedMilliseconds > LongRunningThresholdMilliseconds)
        {
            logger.LongRunningRequest(typeof(TRequest).Name, elapsedMilliseconds, user.Id);
        }

        return response;
    }
}
