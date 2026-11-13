using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Statistics.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Queries.GetTopDonations;

// Items ranked by donated quantity, value and frequency within a scope. Computed live from Donation
// transactions (not materialized), and shaped like GetTopPurchasesQuery so the same chart renders both.
[Authorize]
public class GetTopDonationsQuery : IRequest<Result<TopPurchasesDto>>, ICacheableQuery
{
    public const int DefaultTop = 10;
    public const int MaxTop = 50;

    public PurchaseStatisticsScope Scope { get; init; } = PurchaseStatisticsScope.Last365Days;

    // Required when Scope == Class; ignored otherwise.
    public Guid? ClassId { get; init; }

    public Guid? CategoryId { get; init; }

    public int Top { get; init; } = DefaultTop;

    // Item edits/merges already invalidate the purchase tag; new donations invalidate the donation tag.
    public IReadOnlyCollection<string> Tags => [CacheConstants.PurchaseStatisticsTag, CacheConstants.DonationStatisticsTag];

    public bool BypassCache => false;

    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(10), 30);

    public string BuildCacheKey() =>
        $"{CacheConstants.DonationStatisticsTag}:top:{Scope}:" +
        $"class:{(Scope == PurchaseStatisticsScope.Class ? ClassId?.ToString() : null) ?? "all"}:" +
        $"category:{CategoryId?.ToString() ?? "all"}:top:{Top}";
}
