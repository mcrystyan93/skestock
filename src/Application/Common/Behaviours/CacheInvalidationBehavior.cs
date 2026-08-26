using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using skestock.Application.Common.Caching;

namespace skestock.Application.Common.Behaviours;

public class CacheInvalidationBehavior<TRequest, TResponse>(
    HybridCache cache,
    ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
: IPipelineBehavior<TRequest, TResponse> where TRequest: notnull, IMessage, ICacheInvalidation
{
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(message, cancellationToken);

        // Only continue if the response is a ResultBase and indicates success
        if (response is IResultBase { IsSuccess: false }) return response;

        if (message.Tags.Count > 0)
        {
            logger.LogInformation("Invalidating cache tags: {Tags}.", string.Join(", ", message.Tags));
            await cache.RemoveByTagAsync(message.Tags, cancellationToken);
        }

        return response;
    }
}
