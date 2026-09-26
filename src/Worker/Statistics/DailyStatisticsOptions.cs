namespace Worker.Statistics;

public sealed class DailyStatisticsOptions
{
    public string RunAt { get; set; } = "21:00";

    public string TimeZone { get; set; } = skestock.Shared.Services.BusinessTimeZoneId;

    // Maximum number of local days (including today) recomputed by a single run.
    public int MaxBackfillDays { get; set; } = 31;
}
