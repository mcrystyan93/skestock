using Mediator;
using skestock.Application.Features.Statistics.Commands.MaterializeDailyConsumption;
using skestock.Application.Features.Statistics.Commands.MaterializePurchaseStatistics;
using skestock.Application.Features.Statistics.Queries.GetConsumptionBackfillStart;

namespace Worker.Statistics;

/// <summary>
/// The work performed by one daily statistics run: materialize daily consumption for the
/// affected local days, then refresh purchase statistics.
/// </summary>
/// <remarks>
/// Scheduling, locking, retries and run-state persistence are handled by
/// <see cref="DailyStatisticsService"/>; this class only talks to the Application layer.
/// </remarks>
internal sealed class DailyStatisticsJob(
    DailyStatisticsOptions options,
    TimeZoneInfo timeZone,
    TimeProvider timeProvider,
    ILogger logger)
{
    public async Task RunAsync(
        ISender sender,
        DailyStatisticsJobContext context,
        CancellationToken cancellationToken)
    {
        var (fromDate, toDate) = await GetConsumptionRangeAsync(sender, context, cancellationToken);
        await MaterializeDailyConsumptionAsync(sender, fromDate, toDate, cancellationToken);
        await MaterializePurchaseStatisticsAsync(sender, cancellationToken);
    }

    private async Task<(DateOnly FromDate, DateOnly ToDate)> GetConsumptionRangeAsync(
        ISender sender,
        DailyStatisticsJobContext context,
        CancellationToken cancellationToken)
    {
        var (fromDate, toDate) = ConsumptionRangeCalculator.Calculate(
            timeProvider.GetUtcNow(),
            context.LastSucceededAtUtc,
            timeZone,
            options.MaxBackfillDays);

        // Existing history older than the regular range (e.g. on first publish) is backfilled
        // once. An interrupted backfill leaves the gap in place, so the next run resumes it.
        var backfillStart = await sender.Send(
            new GetConsumptionBackfillStartQuery { TimeZoneId = options.TimeZone },
            cancellationToken);
        backfillStart.ThrowIfFailed();

        if (backfillStart.Value is { } historyStart && historyStart < fromDate)
        {
            logger.LogInformation("Backfilling daily consumption history from {FromDate}.", historyStart);
            fromDate = historyStart;
        }

        return (fromDate, toDate);
    }

    // The command accepts a bounded number of days, so long ranges are sent in consecutive
    // windows, oldest first. A failed window aborts the run; earlier windows stay committed.
    private async Task MaterializeDailyConsumptionAsync(
        ISender sender,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var windows = ConsumptionRangeCalculator.SplitIntoWindows(
            fromDate,
            toDate,
            MaterializeDailyConsumptionCommand.MaxDays);

        foreach (var (windowFrom, windowTo) in windows)
        {
            var result = await sender.Send(
                new MaterializeDailyConsumptionCommand
                {
                    FromDate = windowFrom,
                    ToDate = windowTo,
                    TimeZoneId = options.TimeZone
                },
                cancellationToken);
            result.ThrowIfFailed();

            logger.LogInformation(
                "Materialized {RowCount} daily consumption rows for {FromDate}..{ToDate}.",
                result.Value,
                windowFrom,
                windowTo);
        }
    }

    // Purchase statistics are secondary: a failure is logged without failing the whole run,
    // so it never triggers a retry of the (already committed) consumption work.
    private async Task MaterializePurchaseStatisticsAsync(ISender sender, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new MaterializePurchaseStatisticsCommand { TimeZoneId = options.TimeZone },
                cancellationToken);

            if (result.IsFailed)
            {
                logger.LogError("Purchase statistics materialization failed: {Errors}", result.JoinErrorMessages());
                return;
            }

            logger.LogInformation("Materialized {RowCount} purchase statistics rows.", result.Value);
        }
        catch (Exception ex) when (!IsShutdown(ex, cancellationToken))
        {
            logger.LogError(ex, "Purchase statistics materialization failed.");
        }
    }

    private static bool IsShutdown(Exception exception, CancellationToken cancellationToken) =>
        exception is OperationCanceledException && cancellationToken.IsCancellationRequested;
}
