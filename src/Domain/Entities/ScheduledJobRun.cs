using skestock.Domain.Enums;

namespace skestock.Domain.Entities;

public class ScheduledJobRun
{
    public string JobName { get; set; } = null!;

    public ScheduledJobRunStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? NextRetryAt { get; set; }

    public DateTimeOffset? LastSucceededAt { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }

    public string? LastError { get; set; }
}
