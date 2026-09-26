using Mediator;
using skestock.Application.Features.ScheduledJobs.Commands.RecordScheduledJobRun;
using skestock.Application.Features.ScheduledJobs.Models;
using skestock.Application.Features.ScheduledJobs.Queries.GetScheduledJobLastRun;
using skestock.Domain.Enums;
using SharedServices = skestock.Shared.Services;

namespace Worker.Statistics;

/// <summary>
/// Reads and writes the persisted run state of the daily statistics job.
/// </summary>
/// <remarks>
/// Every call uses its own DI scope (and therefore its own DbContext), so a failed job whose
/// scope holds a broken unit of work can still record its failure state.
/// </remarks>
internal sealed class DailyStatisticsRunStore(IServiceScopeFactory scopeFactory)
{
    public async Task<ScheduledJobRunDto?> GetLastRunAsync(CancellationToken cancellationToken)
    {
        var result = await SendInNewScopeAsync(
            new GetScheduledJobLastRunQuery { JobName = SharedServices.DailyStatisticsJobName },
            cancellationToken);

        result.ThrowIfFailed();
        return result.Value;
    }

    public Task MarkRunningAsync(
        DateTimeOffset attemptedAtUtc,
        int attemptNumber,
        CancellationToken cancellationToken) =>
        RecordAsync(
            attemptedAtUtc,
            succeededAtUtc: null,
            ScheduledJobRunStatus.Running,
            attemptNumber,
            error: null,
            nextRetryAtUtc: null,
            cancellationToken);

    public Task MarkSucceededAsync(
        DateTimeOffset attemptedAtUtc,
        DateTimeOffset succeededAtUtc,
        int attemptNumber,
        CancellationToken cancellationToken) =>
        RecordAsync(
            attemptedAtUtc,
            succeededAtUtc,
            ScheduledJobRunStatus.Succeeded,
            attemptNumber,
            error: null,
            nextRetryAtUtc: null,
            cancellationToken);

    /// <param name="nextRetryAtUtc"><see langword="null"/> when no retry is left.</param>
    public Task MarkFailedAsync(
        DateTimeOffset failedAtUtc,
        int attemptNumber,
        string error,
        DateTimeOffset? nextRetryAtUtc,
        CancellationToken cancellationToken) =>
        RecordAsync(
            failedAtUtc,
            succeededAtUtc: null,
            ScheduledJobRunStatus.Failed,
            attemptNumber,
            error,
            nextRetryAtUtc,
            cancellationToken);

    private async Task RecordAsync(
        DateTimeOffset attemptedAtUtc,
        DateTimeOffset? succeededAtUtc,
        ScheduledJobRunStatus status,
        int attemptNumber,
        string? error,
        DateTimeOffset? nextRetryAtUtc,
        CancellationToken cancellationToken)
    {
        var result = await SendInNewScopeAsync(
            new RecordScheduledJobRunCommand
            {
                JobName = SharedServices.DailyStatisticsJobName,
                AttemptedAtUtc = attemptedAtUtc,
                SucceededAtUtc = succeededAtUtc,
                Error = error,
                Status = status,
                AttemptCount = attemptNumber,
                NextRetryAtUtc = nextRetryAtUtc
            },
            cancellationToken);

        result.ThrowIfFailed();
    }

    private async Task<TResponse> SendInNewScopeAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(request, cancellationToken);
    }
}
