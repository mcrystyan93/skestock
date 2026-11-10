using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.SupplyLists;

namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

public sealed class SupplyListLineItemMerger(IApplicationDbContext dbContext)
    : KeyedItemMerger<SupplyListLine, Guid>(ItemMergeKind.SupplyListLines)
{
    protected override DbSet<SupplyListLine> Rows => dbContext.SupplyListLines;

    protected override Guid ItemIdOf(SupplyListLine row) => row.ItemId;

    protected override void Repoint(SupplyListLine row, Guid primaryId) => row.ItemId = primaryId;

    protected override Guid KeyOf(SupplyListLine row) => row.SupplyListId;

    protected override void Combine(SupplyListLine survivor, SupplyListLine other) => survivor.Quantity += other.Quantity;
}
