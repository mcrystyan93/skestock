using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

public sealed class ItemPurchaseStatisticItemMerger(IApplicationDbContext dbContext)
    : KeyedItemMerger<ItemPurchaseStatistic, (PurchaseStatisticsScope Scope, Guid? ClassId)>(ItemMergeKind.ItemPurchaseStatistics)
{
    protected override DbSet<ItemPurchaseStatistic> Rows => dbContext.ItemPurchaseStatistics;

    protected override Guid ItemIdOf(ItemPurchaseStatistic row) => row.ItemId;

    protected override void Repoint(ItemPurchaseStatistic row, Guid primaryId) => row.ItemId = primaryId;

    protected override (PurchaseStatisticsScope Scope, Guid? ClassId) KeyOf(ItemPurchaseStatistic row) =>
        (row.Scope, row.ClassId);

    // Averages follow PurchaseStatisticsCalculator. AverageIntervalDays stays the survivor's:
    // shortcut: it is not exactly recomputable from totals; the nightly MaterializePurchaseStatistics rebuild fixes it.
    protected override void Combine(ItemPurchaseStatistic survivor, ItemPurchaseStatistic other)
    {
        survivor.TotalQuantity += other.TotalQuantity;
        survivor.TotalValue = Math.Round(survivor.TotalValue + other.TotalValue, 2);
        survivor.PurchaseCount += other.PurchaseCount;
        survivor.AverageQuantity = survivor.PurchaseCount == 0
            ? 0m
            : Math.Round((decimal)survivor.TotalQuantity / survivor.PurchaseCount, 2);
        survivor.AverageUnitPrice = survivor.TotalQuantity == 0
            ? 0m
            : Math.Round(survivor.TotalValue / survivor.TotalQuantity, 2);
        if (other.LastPurchasedAt > survivor.LastPurchasedAt)
            survivor.LastPurchasedAt = other.LastPurchasedAt;
        if (other.ComputedAt > survivor.ComputedAt)
            survivor.ComputedAt = other.ComputedAt;
    }
}
