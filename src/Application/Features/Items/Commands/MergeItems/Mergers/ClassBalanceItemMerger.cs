using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Stock;

namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

// No unique index, but one row per (class, item, location) is the domain invariant (see
// CloseSchoolClassCommandHandler.GetOrAddBalance), so colliding rows are summed instead of re-pointed.
public sealed class ClassBalanceItemMerger(IApplicationDbContext dbContext)
    : KeyedItemMerger<ClassBalance, (Guid ClassId, Guid LocationId)>(ItemMergeKind.ClassBalances)
{
    protected override DbSet<ClassBalance> Rows => dbContext.ClassBalances;

    protected override Guid ItemIdOf(ClassBalance row) => row.ItemId;

    protected override void Repoint(ClassBalance row, Guid primaryId) => row.ItemId = primaryId;

    protected override (Guid ClassId, Guid LocationId) KeyOf(ClassBalance row) => (row.ClassId, row.LocationId);

    protected override void Combine(ClassBalance survivor, ClassBalance other)
    {
        survivor.OpeningQty += other.OpeningQty;
        survivor.ClosingQty = survivor.ClosingQty is null && other.ClosingQty is null
            ? null
            : (survivor.ClosingQty ?? 0) + (other.ClosingQty ?? 0);
    }
}
