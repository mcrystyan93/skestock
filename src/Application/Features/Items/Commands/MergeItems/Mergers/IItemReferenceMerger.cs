namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

/// <summary>Kind of data re-pointed by a merger; the client maps it to a Romanian label.</summary>
public enum ItemMergeKind
{
    StockBatches,
    StockTransactions,
    GoodsReceiptImportLines,
    OrderListLines,
    ClassItemStockVisibilities,
    ItemLocationThresholds,
    SupplyListLines,
    DailyItemConsumptions,
    ClassBalances,
    ItemPurchaseStatistics
}

/// <param name="Relinked">Rows moved to the primary item.</param>
/// <param name="Combined">Rows merged into another row (and deleted) because of a unique key collision.</param>
public sealed record ItemMergeImpact(ItemMergeKind Kind, int Relinked, int Combined);

/// <summary>Moves one table's rows from the secondary items to the primary. One implementation per table.</summary>
public interface IItemReferenceMerger
{
    ItemMergeKind Kind { get; }

    Task<ItemMergeImpact> PreviewAsync(
        Guid primaryId, IReadOnlyCollection<Guid> secondaryIds, CancellationToken cancellationToken);

    /// <summary>Mutates tracked entities only; the caller owns SaveChanges and the transaction.</summary>
    Task MergeAsync(
        Guid primaryId, IReadOnlyCollection<Guid> secondaryIds, CancellationToken cancellationToken);
}
