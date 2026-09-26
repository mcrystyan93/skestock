namespace Worker.Statistics;

public sealed class DailyStatisticsOptions
{
    /// <summary>Format of <see cref="RunAt"/>, validated at startup.</summary>
    public const string RunAtFormat = "HH:mm";

    /// <summary>Local time of day (in <see cref="TimeZone"/>) at which the job runs.</summary>
    public string RunAt { get; set; } = "21:00";

    public string TimeZone { get; set; } = skestock.Shared.Services.BusinessTimeZoneId;

    // Maximum number of local days (including today) recomputed by a single run.
    public int MaxBackfillDays { get; set; } = 31;
}
