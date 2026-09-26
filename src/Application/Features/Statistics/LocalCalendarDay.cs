namespace skestock.Application.Features.Statistics;

public static class LocalCalendarDay
{
    // Returns the UTC instant at which the given local calendar day starts in the time zone.
    public static DateTimeOffset GetStartUtc(DateOnly date, TimeZoneInfo timeZone)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        // Some zones skip midnight on DST transitions; the day then starts at the first valid instant.
        while (timeZone.IsInvalidTime(local))
        {
            local = local.AddMinutes(30);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), TimeSpan.Zero);
    }

    // Returns the local calendar date of an instant in the time zone.
    public static DateOnly GetDate(DateTimeOffset instant, TimeZoneInfo timeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, timeZone).DateTime);
}
