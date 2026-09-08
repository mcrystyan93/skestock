using skestock.Application.Common.Caching;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.SchoolClasses.Models;

namespace skestock.Application.Features.SchoolClasses.Queries.GetAllSchoolClasses;

public class GetAllSchoolClassesQuery: BasePaginationFilter, IRequest<Result<PaginatedResponse<SchoolClassDto>>>, ICacheableQuery
{
    public List<ColumnFilter> Filters { get; init; } = [];
    public IReadOnlyCollection<string> Tags => [CacheConstants.SchoolClassListTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);
    public string BuildCacheKey()
    {
        return $"{CacheConstants.SchoolClass}:list" +
               $"search={CacheKeyNormalization.Text(SearchTerm)}:" +
               $"filters={CacheKeyNormalization.Filters(Filters)}:" +
               $"pageSize={CacheKeyNormalization.Int(PageSize)}:" +
               $"cursor={CacheKeyNormalization.Cursor(Cursor)}:" +
               $"sort={CacheKeyNormalization.Sort(Sort)}";
    }
}
