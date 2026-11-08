using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Stock;

namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

// shortcut: loads every transaction of the secondary items into memory; switch to ExecuteUpdate (also setting LastModifiedById) if a merge ever times out.
public sealed class StockTransactionItemMerger(IApplicationDbContext dbContext) : PlainItemMerger<StockTransaction>(ItemMergeKind.StockTransactions)
{
    protected override IQueryable<StockTransaction> RowsOf(IReadOnlyCollection<Guid> itemIds) =>
        dbContext.StockTransactions.Where(row => itemIds.Contains(row.ItemId));

    protected override void Repoint(StockTransaction row, Guid primaryId) => row.ItemId = primaryId;
}
