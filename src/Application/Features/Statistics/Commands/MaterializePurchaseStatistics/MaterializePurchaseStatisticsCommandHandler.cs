using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
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

        // Received quantity lives on the Order transaction; StockBatch.Quantity is what remains.
        var lines = await dbContext.StockTransactions
            .AsNoTracking()
            .Where(t => t.Type == StockTransactionType.Order && t.QuantityChange > 0)
            .Select(t => new PurchaseLine(
                t.ItemId,
                t.ClassId,
                t.GoodsReceiptId ?? t.Id,
                t.QuantityChange,
                t.Batch != null ? t.Batch.UnitPrice : 0m,
                t.GoodsReceipt != null ? t.GoodsReceipt.ReceivedAt : t.CreatedAt))
            .ToListAsync(cancellationToken);

        var statistics = PurchaseStatisticsCalculator.Calculate(
            lines,
            LocalCalendarDay.GetDate(computedAt, timeZone),
            timeZone,
            computedAt);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(
            async strategyCancellationToken =>
            {
                dbContext.ItemPurchaseStatistics.Local.Clear();

                await using var transaction =
                    await dbContext.Database.BeginTransactionAsync(strategyCancellationToken);

                await dbContext.ItemPurchaseStatistics.ExecuteDeleteAsync(strategyCancellationToken);

                dbContext.ItemPurchaseStatistics.AddRange(statistics.Select(Clone));
                await dbContext.SaveChangesAsync(strategyCancellationToken);
                await transaction.CommitAsync(strategyCancellationToken);
            },
            cancellationToken);

        return Result.Ok(statistics.Count);
    }

    // A fresh instance per strategy attempt, so a retried execution never re-adds tracked entities.
    private static ItemPurchaseStatistic Clone(ItemPurchaseStatistic source) => new()
    {
        Scope = source.Scope,
        ClassId = source.ClassId,
        ItemId = source.ItemId,
        TotalQuantity = source.TotalQuantity,
        TotalValue = source.TotalValue,
        PurchaseCount = source.PurchaseCount,
        AverageQuantity = source.AverageQuantity,
        AverageUnitPrice = source.AverageUnitPrice,
        LastPurchasedAt = source.LastPurchasedAt,
        ComputedAt = source.ComputedAt
    };
}
