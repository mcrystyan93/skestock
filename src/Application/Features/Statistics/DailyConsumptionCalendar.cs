using skestock.Shared;

namespace skestock.Application.Features.Statistics;

public static class DailyConsumptionCalendar
{
    public static TimeZoneInfo TimeZone { get; } =
        TimeZoneInfo.FindSystemTimeZoneById(Services.BusinessTimeZoneId);

    public static DateOnly Today(TimeProvider timeProvider) =>
        LocalCalendarDay.GetDate(timeProvider.GetUtcNow(), TimeZone);

    // Cache keys include the local date so cached rolling windows expire at local midnight.
    public static string CurrentDateKey() =>
        Today(TimeProvider.System).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
