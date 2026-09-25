namespace Worker.Statistics;

public sealed class DailyStatisticsOptions
{
    public string RunAt { get; set; } = "21:00";

    public string TimeZone { get; set; } = "Europe/Bucharest";
}
