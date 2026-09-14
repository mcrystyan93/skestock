using skestock.Application.Common.Caching;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Categories.Queries.GetAllCategoryImportBatches;

public class GetAllCategoryImportBatchesQuery : BasePaginationFilter,
    IRequest<Result<PaginatedResponse<CategoryImportBatchListItemDto>>>, ICacheableQuery
{
    public List<ColumnFilter> Filters { get; init; } = [];
    public IReadOnlyCollection<string> Tags => [CacheConstants.CategoryImportBatchListTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() =>
        $"{CacheConstants.CategoryImportBatch}:list" +
        $"search={CacheKeyNormalization.Text(SearchTerm)}:" +
        $"filters={CacheKeyNormalization.Filters(Filters)}:" +
        $"pageSize={CacheKeyNormalization.Int(PageSize)}:" +
        $"cursor={CacheKeyNormalization.Cursor(Cursor)}:" +
        $"sort={CacheKeyNormalization.Sort(Sort)}";
}
