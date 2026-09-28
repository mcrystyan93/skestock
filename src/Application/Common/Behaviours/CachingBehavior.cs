using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using skestock.Application.Common.Caching;

namespace skestock.Application.Common.Behaviours;

public class CachingBehavior<TRequest, TResponse>(
    HybridCache cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage, ICacheableQuery
{
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        if (message.BypassCache)
        {
            return await next(message, cancellationToken);
        }

        var cacheKey = message.BuildCacheKey();
        var cachedResult = await GetOrCreateAsync(message, next, cacheKey, cancellationToken);

        try
        {
            return ResultCacheTransformer.Deserialize<TResponse>(cachedResult);
        }
        catch (Exception ex) when (IsUnreadablePayload(ex))
        {
            // The distributed cache outlives deployments, so an entry may have been written with an
            // older DTO shape. Treat it as a miss instead of failing the request.
            logger.UnreadableCacheEntry(cacheKey, typeof(TRequest).Name);
            await cache.RemoveAsync(cacheKey, cancellationToken);

            cachedResult = await GetOrCreateAsync(message, next, cacheKey, cancellationToken);
            return ResultCacheTransformer.Deserialize<TResponse>(cachedResult);
        }
    }

    private ValueTask<string> GetOrCreateAsync(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        string cacheKey,
        CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var result = await next(message, token);
                return ResultCacheTransformer.Serialize(result!);
            },
            new HybridCacheEntryOptions { Expiration = message.SlidingExpiration ?? TimeSpan.FromMinutes(5) },
            tags: message.Tags,
            cancellationToken: cancellationToken);

    // ResultCacheTransformer deserializes through reflection, which wraps the JsonException.
    private static bool IsUnreadablePayload(Exception exception) =>
        exception is JsonException or TargetInvocationException { InnerException: JsonException };
}
