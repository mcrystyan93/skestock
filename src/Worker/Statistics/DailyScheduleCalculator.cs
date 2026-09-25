using System.Globalization;

namespace Worker.Statistics;

public sealed class DailyScheduleCalculator
{
    private readonly TimeOnly _runAt;
    private readonly TimeZoneInfo _timeZone;

    public DailyScheduleCalculator(DailyStatisticsOptions options)
        : this(
            TimeOnly.ParseExact(options.RunAt, "HH:mm", CultureInfo.InvariantCulture),
            TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone))
    {
    }

    public DailyScheduleCalculator(TimeOnly runAt, TimeZoneInfo timeZone)
    {
        _runAt = runAt;
        _timeZone = timeZone;
    }

    public DateTimeOffset GetNextOccurrence(DateTimeOffset utcNow)
    {
        var now = utcNow.ToUniversalTime();
        var localNow = TimeZoneInfo.ConvertTime(now, _timeZone);
        var localDate = DateOnly.FromDateTime(localNow.DateTime);
        var today = GetOccurrence(localDate);

        return today > now
            ? today
            : GetOccurrence(localDate.AddDays(1));
    }

    public DateTimeOffset GetMostRecentDueOccurrence(DateTimeOffset utcNow)
    {
        var now = utcNow.ToUniversalTime();
        var localNow = TimeZoneInfo.ConvertTime(now, _timeZone);
        var localDate = DateOnly.FromDateTime(localNow.DateTime);
        var today = GetOccurrence(localDate);

        return today <= now
            ? today
            : GetOccurrence(localDate.AddDays(-1));
    }

    private DateTimeOffset GetOccurrence(DateOnly date)
    {
        var localDateTime = date.ToDateTime(_runAt, DateTimeKind.Unspecified);

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
