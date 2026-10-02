using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.ClassConfiguration.Models;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetDepartments;

[Authorize]
public sealed class GetDepartmentsQuery : IRequest<Result<IReadOnlyList<DepartmentTemplateDto>>>, ICacheableQuery
{
    public IReadOnlyCollection<string> Tags => [CacheConstants.ConfigurationTag, CacheConstants.DepartmentListTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
    public string BuildCacheKey() => CacheConstants.DepartmentListTag;
}
