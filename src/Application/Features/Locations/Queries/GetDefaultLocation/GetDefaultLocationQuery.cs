using skestock.Application.Common.Caching;
using skestock.Application.Features.Locations.Models;

namespace skestock.Application.Features.Locations.Queries.GetDefaultLocation;

public class GetDefaultLocationQuery
    : IRequest<Result<LocationDto?>>, ICacheableQuery
{
    // Shares the list tag so existing Create/Update cache invalidation also busts the default entry;
    // the dedicated default tag is future-proofing for a potential "set default" command.
    public IReadOnlyCollection<string> Tags => [CacheConstants.LocationListTag, CacheConstants.LocationDefaultTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() => $"{CacheConstants.Location}:default";
}
