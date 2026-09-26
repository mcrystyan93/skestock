namespace Worker.Statistics;

public static class ConsumptionRangeCalculator
{
    // Recomputes from the day before the last successful run (so that day gets finalized) up to
    // today (provisional). Without a previous success, or after a long outage, the range is capped
    // to the most recent maxDays local days.
    public static (DateOnly FromDate, DateOnly ToDate) Calculate(
        DateTimeOffset utcNow,
        DateTimeOffset? lastSucceededAtUtc,
        TimeZoneInfo timeZone,
        int maxDays)
    {
        var today = ToLocalDate(utcNow, timeZone);
        var earliest = today.AddDays(-(maxDays - 1));

        var from = lastSucceededAtUtc is { } lastSucceededAt
            ? ToLocalDate(lastSucceededAt, timeZone).AddDays(-1)
            : earliest;

        if (from < earliest)
        {
            from = earliest;
        }

        if (from > today)
        {
            from = today;
        }

        return (from, today);
    }

    private static DateOnly ToLocalDate(DateTimeOffset instant, TimeZoneInfo timeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, timeZone).DateTime);
}
