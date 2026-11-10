using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.OrderLists;

namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

public sealed class OrderListLineItemMerger(IApplicationDbContext dbContext) : PlainItemMerger<OrderListLine>(ItemMergeKind.OrderListLines)
{
    protected override IQueryable<OrderListLine> RowsOf(IReadOnlyCollection<Guid> itemIds) =>
        dbContext.OrderListLines.Where(row => row.ItemId != null && itemIds.Contains(row.ItemId.Value));

    protected override void Repoint(OrderListLine row, Guid primaryId) => row.ItemId = primaryId;
}
