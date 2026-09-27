using skestock.Application.Common.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Commands.MaterializePurchaseStatistics;

public sealed class MaterializePurchaseStatisticsCommandHandler(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider)
    : IRequestHandler<MaterializePurchaseStatisticsCommand, Result<int>>
{
    public async ValueTask<Result<int>> Handle(
        MaterializePurchaseStatisticsCommand request,
        CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
        var computedAt = timeProvider.GetUtcNow();

        // Only goods receipts are purchases: manually created Order transactions (e.g. stock batches
        // entered outside a receipt) are excluded. Received quantity lives on the receipt's Order
        // transaction; StockBatch.Quantity is what remains.
        var lines = await dbContext.StockTransactions
            .AsNoTracking()
            .Where(t => t.Type == StockTransactionType.Order
                        && t.QuantityChange > 0
                        && t.GoodsReceipt != null)
            .Select(t => new PurchaseLine(
                t.ItemId,
                t.GoodsReceipt!.ClassId,
                t.GoodsReceipt.Id,
                t.QuantityChange,
                t.Batch != null ? t.Batch.UnitPrice : 0m,
                t.GoodsReceipt.ReceivedAt))
            .ToListAsync(cancellationToken);

        var today = LocalCalendarDay.GetDate(computedAt, timeZone);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        var count = await strategy.ExecuteAsync(
            async strategyCancellationToken =>
            {
                dbContext.ItemPurchaseStatistics.Local.Clear();

                // Calculated per attempt so a retried execution adds fresh, untracked entities.
                var statistics = PurchaseStatisticsCalculator.Calculate(lines, today, timeZone, computedAt);

                await using var transaction =
                    await dbContext.Database.BeginTransactionAsync(strategyCancellationToken);

                await dbContext.ItemPurchaseStatistics.ExecuteDeleteAsync(strategyCancellationToken);

                dbContext.ItemPurchaseStatistics.AddRange(statistics);
                await dbContext.SaveChangesAsync(strategyCancellationToken);
                await transaction.CommitAsync(strategyCancellationToken);

                return statistics.Count;
            },
            cancellationToken);

        return Result.Ok(count);
    }
}
