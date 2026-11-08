using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Stock;

namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

public sealed class ClassItemStockVisibilityItemMerger(IApplicationDbContext dbContext)
    : KeyedItemMerger<ClassItemStockVisibility, (Guid ClassId, Guid LocationId)>(ItemMergeKind.ClassItemStockVisibilities)
{
    protected override DbSet<ClassItemStockVisibility> Rows => dbContext.ClassItemStockVisibilities;

    protected override Guid ItemIdOf(ClassItemStockVisibility row) => row.ItemId;

    protected override void Repoint(ClassItemStockVisibility row, Guid primaryId) => row.ItemId = primaryId;

    protected override (Guid ClassId, Guid LocationId) KeyOf(ClassItemStockVisibility row) => (row.ClassId, row.LocationId);
}
