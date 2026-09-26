using skestock.Application.Common.Caching;

namespace skestock.Application.Features.Statistics.Commands.MaterializePurchaseStatistics;

// Recomputes every ItemPurchaseStatistic row (rolling windows and per-class history) from the
// received stock lines. All existing rows are replaced, so re-running is idempotent.
public sealed class MaterializePurchaseStatisticsCommand : IRequest<Result<int>>, ICacheInvalidation
{
    // IANA/Windows time zone id that defines the calendar day boundaries of the rolling windows.
    public string TimeZoneId { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Tags => [CacheConstants.PurchaseStatisticsTag];
}
