namespace skestock.Application.Common.Caching;

/// <summary>
/// Marker interface for commands that invalidate cached query results. Tags listed here are
/// removed via HybridCache.RemoveByTagAsync after the command succeeds — see
/// <see cref="skestock.Application.Common.Behaviours.CacheInvalidationBehavior{TRequest,TResponse}"/>.
/// </summary>
public interface ICacheInvalidation
{
    IReadOnlyCollection<string> Tags { get; }
}
