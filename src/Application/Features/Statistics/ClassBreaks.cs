namespace skestock.Application.Features.Statistics;

public sealed record ClassPeriod(DateOnly StartDate, DateOnly EndDate);

public static class ClassBreaks
{
    // Time inside [from, to] that falls between class ranges (overlapping or adjacent classes merge).
    // Time before the earliest start or after the latest end is not a break.
    public static TimeSpan Between(
        IReadOnlyCollection<ClassPeriod> periods,
        DateTimeOffset from,
        DateTimeOffset to,
        TimeZoneInfo timeZone)
    {
        if (to <= from)
        {
            return TimeSpan.Zero;
        }

        var merged = new List<ClassPeriod>();
        foreach (var period in periods.OrderBy(p => p.StartDate))
        {
            if (merged.Count > 0 && period.StartDate <= merged[^1].EndDate.AddDays(1))
            {
                var last = merged[^1];
                merged[^1] = last with { EndDate = period.EndDate > last.EndDate ? period.EndDate : last.EndDate };
            }
            else
            {
                merged.Add(period);
            }
        }

        var total = TimeSpan.Zero;
        for (var i = 1; i < merged.Count; i++)
        {
            var gapStart = LocalCalendarDay.GetStartUtc(merged[i - 1].EndDate.AddDays(1), timeZone);
            var gapEnd = LocalCalendarDay.GetStartUtc(merged[i].StartDate, timeZone);
            var overlap = (gapEnd < to ? gapEnd : to) - (gapStart > from ? gapStart : from);
            if (overlap > TimeSpan.Zero)
            {
                total += overlap;
            }
        }

        return total;
    }
}
