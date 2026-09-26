using skestock.Application.Common.Caching;

namespace skestock.Application.Features.Statistics.Commands.MaterializeDailyConsumption;

// Recomputes the daily item consumption totals for every local calendar day in
// [FromDate, ToDate]. Existing rows in that range are replaced, so re-running is idempotent.
public sealed class MaterializeDailyConsumptionCommand : IRequest<Result<int>>, ICacheInvalidation
{
    public const int MaxDays = 31;

    public DateOnly FromDate { get; init; }

    public DateOnly ToDate { get; init; }

    // IANA/Windows time zone id that defines the calendar day boundaries.
    public string TimeZoneId { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Tags => [CacheConstants.DailyConsumptionTag];
}
