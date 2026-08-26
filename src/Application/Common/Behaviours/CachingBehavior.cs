using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Caching;

namespace skestock.Application.Common.Behaviours;

        
public class CachingBehavior<TRequest, TResponse>(HybridCache cache): IPipelineBehavior<TRequest, Result<TResponse>>
    where TRequest : notnull, IMessage, ICacheableQuery<TResponse>
{
    public async ValueTask<Result<TResponse>> Handle(TRequest message, MessageHandlerDelegate<TRequest, Result<TResponse>> next, CancellationToken cancellationToken)
    {
        if (message.BypassCache)
        {
            return await next(message, cancellationToken);
        }

        var cacheKey = message.BuildCacheKey();
        var slidingExpiration = message.SlidingExpiration ?? TimeSpan.FromMinutes(5);

        var cachedResult = await cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var result = await next(message, token);
                return result.ToResultCache();
            },
            new HybridCacheEntryOptions { Expiration = slidingExpiration },
            tags: message.Tags,
            cancellationToken: cancellationToken);

        return cachedResult.ToResult();
    }
}
