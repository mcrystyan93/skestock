using skestock.Application.Common.Caching;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Categories.Queries.GetAllCategories;

public class GetAllCategoriesQuery: BasePaginationFilter, IRequest<Result<PaginatedResponse<CategoryDto>>>, ICacheableQuery
{
    public List<ColumnFilter> Filters { get; init; } = [];
    public IReadOnlyCollection<string> Tags => [CacheConstants.CategoryListTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);
    public string BuildCacheKey()
    {
        return $"{CacheConstants.Category}:list" +
               $"search={CacheKeyNormalization.Text(SearchTerm)}:" +
               $"filters={CacheKeyNormalization.Filters(Filters)}:" +
               $"pageSize={CacheKeyNormalization.Int(PageSize)}:" +
               $"cursor={CacheKeyNormalization.Cursor(Cursor)}:" +
               $"sort={CacheKeyNormalization.Sort(Sort)}";
    }
}
