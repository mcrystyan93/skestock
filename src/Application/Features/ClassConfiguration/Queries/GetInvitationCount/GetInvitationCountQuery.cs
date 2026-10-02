using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.ClassConfiguration.Models;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetInvitationCount;

[Authorize]
public sealed class GetInvitationCountQuery : IRequest<Result<InvitationCountDto>>, ICacheableQuery
{
    public IReadOnlyCollection<string> Tags => [CacheConstants.ConfigurationTag, CacheConstants.InvitationCountTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
    public string BuildCacheKey() => CacheConstants.InvitationCountTag;
}
