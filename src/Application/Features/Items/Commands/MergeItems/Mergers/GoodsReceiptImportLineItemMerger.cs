using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.GoodsReceipts;

namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

public sealed class GoodsReceiptImportLineItemMerger(IApplicationDbContext dbContext) : PlainItemMerger<GoodsReceiptImportLine>(ItemMergeKind.GoodsReceiptImportLines)
{
    protected override IQueryable<GoodsReceiptImportLine> RowsOf(IReadOnlyCollection<Guid> itemIds) =>
        dbContext.GoodsReceiptImportLines.Where(row => row.MatchedItemId != null && itemIds.Contains(row.MatchedItemId.Value));

    protected override void Repoint(GoodsReceiptImportLine row, Guid primaryId) => row.MatchedItemId = primaryId;
}
