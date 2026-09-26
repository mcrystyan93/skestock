namespace Worker.Statistics;

/// <summary>Input for one daily statistics run.</summary>
/// <param name="Occurrence">The scheduled occurrence (UTC instant) being processed.</param>
/// <param name="LastSucceededAtUtc">When the job last succeeded, or <see langword="null"/> if never.</param>
public sealed record DailyStatisticsJobContext(
    DateTimeOffset Occurrence,
    DateTimeOffset? LastSucceededAtUtc);
