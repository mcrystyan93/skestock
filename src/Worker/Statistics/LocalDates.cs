namespace Worker.Statistics;

internal static class LocalDates
{
    /// <summary>The calendar date of <paramref name="instant"/> in <paramref name="timeZone"/>.</summary>
    public static DateOnly ToLocalDate(DateTimeOffset instant, TimeZoneInfo timeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, timeZone).DateTime);
}
