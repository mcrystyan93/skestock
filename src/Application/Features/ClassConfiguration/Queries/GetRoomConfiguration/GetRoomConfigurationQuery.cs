using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.ClassConfiguration.Models;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetRoomConfiguration;

[Authorize]
public sealed class GetRoomConfigurationQuery : IRequest<Result<RoomConfigurationDto>>, ICacheableQuery
{
    public IReadOnlyCollection<string> Tags => [CacheConstants.ConfigurationTag, CacheConstants.RoomConfigurationTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
    public string BuildCacheKey() => CacheConstants.RoomConfigurationTag;
}
