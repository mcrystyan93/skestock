using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.ClassConfiguration.Models;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetDepartment;

[Authorize]
public sealed class GetDepartmentQuery : IRequest<Result<DepartmentTemplateDto>>, ICacheableQuery
{
    public Guid Id { get; init; }
    public IReadOnlyCollection<string> Tags => [CacheConstants.ConfigurationTag, CacheConstants.DepartmentListTag, BuildCacheKey()];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
    public string BuildCacheKey() => $"{CacheConstants.DepartmentListTag}:{Id}";
}
