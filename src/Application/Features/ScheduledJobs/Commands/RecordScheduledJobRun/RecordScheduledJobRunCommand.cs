using skestock.Domain.Enums;

namespace skestock.Application.Features.ScheduledJobs.Commands.RecordScheduledJobRun;

public sealed class RecordScheduledJobRunCommand : IRequest<Result>
{
    public string JobName { get; init; } = string.Empty;

    public ScheduledJobRunStatus Status { get; init; }

    public int AttemptCount { get; init; }

    public DateTimeOffset AttemptedAtUtc { get; init; }

    public DateTimeOffset? SucceededAtUtc { get; init; }

    public DateTimeOffset? NextRetryAtUtc { get; init; }

    public string? Error { get; init; }
}
