namespace skestock.Application.Features.Statistics.Queries.GetConsumptionBackfillStart;

// Returns the local date from which daily consumption must be backfilled: the date of the earliest
// consumption transaction when it precedes every materialized DailyItemConsumption row (or none
// exist). Returns null when history is already covered. Deliberately not cached.
public sealed class GetConsumptionBackfillStartQuery : IRequest<Result<DateOnly?>>
{
    // IANA/Windows time zone id that defines the calendar day boundaries.
    public string TimeZoneId { get; init; } = string.Empty;
}
