using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Items;

namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

public sealed class ItemLocationThresholdItemMerger(IApplicationDbContext dbContext)
    : KeyedItemMerger<ItemLocationThreshold, Guid>(ItemMergeKind.ItemLocationThresholds)
{
    protected override DbSet<ItemLocationThreshold> Rows => dbContext.ItemLocationThresholds;

    protected override Guid ItemIdOf(ItemLocationThreshold row) => row.ItemId;

    protected override void Repoint(ItemLocationThreshold row, Guid primaryId) => row.ItemId = primaryId;

    protected override Guid KeyOf(ItemLocationThreshold row) => row.LocationId;
}
