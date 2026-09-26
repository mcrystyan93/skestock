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
        var today = LocalDates.ToLocalDate(utcNow, timeZone);
        var earliest = today.AddDays(-(maxDays - 1));

        var preferredFrom = lastSucceededAtUtc is { } lastSucceededAt
            ? LocalDates.ToLocalDate(lastSucceededAt, timeZone).AddDays(-1)
            : earliest;

        return (Clamp(preferredFrom, earliest, today), today);
    }

    // Splits [from, to] into consecutive, non-overlapping windows of at most maxDays days, oldest first.
    public static IReadOnlyList<(DateOnly FromDate, DateOnly ToDate)> SplitIntoWindows(
        DateOnly from,
        DateOnly to,
        int maxDays)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDays, 1);

        var windows = new List<(DateOnly, DateOnly)>();
        for (var start = from; start <= to; start = start.AddDays(maxDays))
        {
            var end = start.AddDays(maxDays - 1);
            windows.Add((start, end < to ? end : to));
        }

        return windows;
    }

    // Lower bound first, so a (misconfigured) earliest > today still yields today.
    private static DateOnly Clamp(DateOnly value, DateOnly min, DateOnly max)
    {
        if (value < min)
        {
            value = min;
        }

        return value > max ? max : value;
    }
}
