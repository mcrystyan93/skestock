using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.ClassConfiguration.Models;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetInvitationSplit;

[Authorize]
public sealed class GetInvitationSplitQuery : IRequest<Result<InvitationSplitDto>>, ICacheableQuery
{
    public IReadOnlyCollection<string> Tags => [CacheConstants.ConfigurationTag, CacheConstants.InvitationSplitTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
    public string BuildCacheKey() => CacheConstants.InvitationSplitTag;
}
