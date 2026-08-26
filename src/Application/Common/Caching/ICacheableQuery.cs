namespace skestock.Application.Common.Caching;

/// <summary>
/// Marker interface for cacheable queries. TResponse is the inner value type of Result{TResponse}.
/// CachingBehavior is constrained to this interface, so only requests implementing it
/// will be intercepted — no runtime guard needed.
/// </summary>
public interface ICacheableQuery<TResponse>
{
    /// <summary>
    /// Tags this cache entry is stamped with. Used by <see cref="ICacheInvalidation.Tags"/>
    /// (via HybridCache.RemoveByTagAsync) to invalidate every entry sharing a tag, without
    /// tracking individual cache keys.
    /// </summary>
    IReadOnlyCollection<string> Tags { get; }
    bool BypassCache { get; }
    TimeSpan? SlidingExpiration { get; }
    string BuildCacheKey();
}
