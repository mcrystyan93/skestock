using skestock.Application.Common.Caching;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.GoodsReceipts.Models;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetAllGoodsReceipts;

public class GetAllGoodsReceiptsQuery : BasePaginationFilter, IRequest<Result<PaginatedResponse<GoodsReceiptListItemDto>>>, ICacheableQuery
{
    public List<ColumnFilter> Filters { get; init; } = [];
    public IReadOnlyCollection<string> Tags => [CacheConstants.GoodsReceiptListTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey()
    {
        return $"{CacheConstants.GoodsReceipt}:list" +
               $"search={CacheKeyNormalization.Text(SearchTerm)}:" +
               $"filters={CacheKeyNormalization.Filters(Filters)}:" +
               $"pageSize={CacheKeyNormalization.Int(PageSize)}:" +
               $"cursor={CacheKeyNormalization.Cursor(Cursor)}:" +
               $"sort={CacheKeyNormalization.Sort(Sort)}";
    }
}
