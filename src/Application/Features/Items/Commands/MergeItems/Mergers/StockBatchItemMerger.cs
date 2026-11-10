using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Stock;

namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

public sealed class StockBatchItemMerger(IApplicationDbContext dbContext) : PlainItemMerger<StockBatch>(ItemMergeKind.StockBatches)
{
    protected override IQueryable<StockBatch> RowsOf(IReadOnlyCollection<Guid> itemIds) =>
        dbContext.StockBatches.Where(row => itemIds.Contains(row.ItemId));

    protected override void Repoint(StockBatch row, Guid primaryId) => row.ItemId = primaryId;
}
