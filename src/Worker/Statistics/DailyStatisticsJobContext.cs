namespace Worker.Statistics;

public sealed record DailyStatisticsJobContext(
    DateTimeOffset Occurrence,
    DateTimeOffset? LastSucceededAtUtc);
