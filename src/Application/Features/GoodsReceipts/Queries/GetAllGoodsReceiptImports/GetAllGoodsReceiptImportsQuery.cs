using skestock.Application.Common.Caching;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.GoodsReceipts.Models;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetAllGoodsReceiptImports;

public class GetAllGoodsReceiptImportsQuery : BasePaginationFilter, IRequest<Result<PaginatedResponse<GoodsReceiptImportListItemDto>>>, ICacheableQuery<PaginatedResponse<GoodsReceiptImportListItemDto>>
{
    public List<ColumnFilter> Filters { get; init; } = [];
    public IReadOnlyCollection<string> Tags => [CacheConstants.GoodsReceiptImportListTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey()
    {
        return $"{CacheConstants.GoodsReceiptImport}:list" +
               $"search={CacheKeyNormalization.Text(SearchTerm)}:" +
               $"filters={CacheKeyNormalization.Filters(Filters)}:" +
               $"pageSize={CacheKeyNormalization.Int(PageSize)}:" +
               $"cursor={CacheKeyNormalization.Cursor(Cursor)}:" +
               $"sort={CacheKeyNormalization.Sort(Sort)}";
    }
}
