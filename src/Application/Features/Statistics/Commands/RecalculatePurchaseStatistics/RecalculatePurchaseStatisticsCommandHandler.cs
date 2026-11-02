using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Commands.MaterializePurchaseStatistics;
using skestock.Shared;

namespace skestock.Application.Features.Statistics.Commands.RecalculatePurchaseStatistics;

public sealed class RecalculatePurchaseStatisticsCommandHandler(
    ISender sender,
    IDistributedLock distributedLock)
    : IRequestHandler<RecalculatePurchaseStatisticsCommand, Result<int>>
{
    // Matches the Worker's lock expiry for the same run.
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(30);

    public async ValueTask<Result<int>> Handle(
        RecalculatePurchaseStatisticsCommand request,
        CancellationToken cancellationToken)
    {
        // Same lock as the daily job: the materialization deletes and re-inserts every row, so two
        // concurrent runs would collide on the unique index.
        await using var lease = await distributedLock.TryAcquireAsync(
            Services.DailyStatisticsLockKey,
            LockExpiry,
            cancellationToken);

        if (lease is null)
        {
            return Result.Fail(new StatisticsErrors.RunInProgress());
        }

        return await sender.Send(
            new MaterializePurchaseStatisticsCommand { TimeZoneId = Services.BusinessTimeZoneId },
            cancellationToken);
    }
}
