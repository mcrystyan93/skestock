using System.Globalization;

namespace Worker.Statistics;

/// <summary>
/// Computes the UTC instants of a job that runs once per local day at a fixed local time.
/// </summary>
public sealed class DailyScheduleCalculator
{
    private readonly TimeOnly _runAt;
    private readonly TimeZoneInfo _timeZone;

    public DailyScheduleCalculator(DailyStatisticsOptions options)
        : this(
            TimeOnly.ParseExact(options.RunAt, DailyStatisticsOptions.RunAtFormat, CultureInfo.InvariantCulture),
            TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone))
    {
    }

    public DailyScheduleCalculator(TimeOnly runAt, TimeZoneInfo timeZone)
    {
        _runAt = runAt;
        _timeZone = timeZone;
    }

    /// <summary>The first occurrence strictly after <paramref name="utcNow"/>.</summary>
    public DateTimeOffset GetNextOccurrence(DateTimeOffset utcNow)
    {
        var localDate = LocalDates.ToLocalDate(utcNow, _timeZone);
        var todaysOccurrence = GetOccurrence(localDate);

        return todaysOccurrence > utcNow
            ? todaysOccurrence
            : GetOccurrence(localDate.AddDays(1));
    }

    /// <summary>The latest occurrence at or before <paramref name="utcNow"/>.</summary>
    public DateTimeOffset GetMostRecentDueOccurrence(DateTimeOffset utcNow)
    {
        var localDate = LocalDates.ToLocalDate(utcNow, _timeZone);
        var todaysOccurrence = GetOccurrence(localDate);

        return todaysOccurrence <= utcNow
            ? todaysOccurrence
            : GetOccurrence(localDate.AddDays(-1));
    }

    private DateTimeOffset GetOccurrence(DateOnly localDate)
    {
        var localDateTime = localDate.ToDateTime(_runAt, DateTimeKind.Unspecified);

        // If a caller configures a time in a DST gap, move forward until the local time
        // exists. Europe/Bucharest's default 21:00 schedule never enters this branch.
        while (_timeZone.IsInvalidTime(localDateTime))
        {
            localDateTime = localDateTime.AddMinutes(1);
        }

        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, _timeZone);
        return new DateTimeOffset(utcDateTime);
    }
}
