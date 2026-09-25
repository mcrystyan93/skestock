using skestock.Domain.Enums;

namespace skestock.Application.Features.ScheduledJobs.Models;

public sealed class ScheduledJobRunDto
{
    public string JobName { get; init; } = string.Empty;

    public ScheduledJobRunStatus Status { get; init; }

    public int AttemptCount { get; init; }

    public DateTimeOffset? NextRetryAt { get; init; }

    public DateTimeOffset? LastSucceededAt { get; init; }

    public DateTimeOffset? LastAttemptAt { get; init; }

    public string? LastError { get; init; }
}
