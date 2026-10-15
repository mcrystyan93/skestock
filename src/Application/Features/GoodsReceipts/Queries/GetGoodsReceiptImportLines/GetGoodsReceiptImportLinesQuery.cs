using skestock.Application.Common.Caching;
using skestock.Application.Features.GoodsReceipts.Models;
using LocationCacheConstants = skestock.Application.Features.Locations.CacheConstants;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptImportLines;

public class GetGoodsReceiptImportLinesQuery : IRequest<Result<List<GoodsReceiptImportLineDto>>>, ICacheableQuery
{
    public Guid Id { get; init; }
    public IReadOnlyCollection<string> Tags =>
        [CacheConstants.GoodsReceiptListTag, CacheConstants.BuildReceiptTag(Id), LocationCacheConstants.LocationListTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);
    public string BuildCacheKey() => $"{CacheConstants.GoodsReceipt}:{Id}:import-lines";
}
