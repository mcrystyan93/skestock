using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Statistics.Models;
using GoodsReceiptCacheConstants = skestock.Application.Features.GoodsReceipts.CacheConstants;

namespace skestock.Application.Features.Statistics.Queries.GetClassGoodsReceiptCosts;

[Authorize]
public class GetClassGoodsReceiptCostsQuery : IRequest<Result<ClassGoodsReceiptCostsDto>>, ICacheableQuery
{
    public Guid ClassId { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }

    public IReadOnlyCollection<string> Tags =>
    [
        GoodsReceiptCacheConstants.GoodsReceiptListTag,
        CacheConstants.BuildClassGoodsReceiptCostTag(ClassId)
    ];

    public bool BypassCache => false;

    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() =>
        $"{CacheConstants.Statistics}:class:{ClassId}:goods-receipt-costs:" +
        $"start={StartDate?.ToString("yyyy-MM-dd") ?? "all"}:" +
        $"end={EndDate?.ToString("yyyy-MM-dd") ?? "all"}";
}
