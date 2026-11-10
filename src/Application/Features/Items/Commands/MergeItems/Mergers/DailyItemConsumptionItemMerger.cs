using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Statistics;

namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

public sealed class DailyItemConsumptionItemMerger(IApplicationDbContext dbContext)
    : KeyedItemMerger<DailyItemConsumption, (DateOnly Date, Guid ClassId, Guid LocationId)>(ItemMergeKind.DailyItemConsumptions)
{
    protected override DbSet<DailyItemConsumption> Rows => dbContext.DailyItemConsumptions;

    protected override Guid ItemIdOf(DailyItemConsumption row) => row.ItemId;

    protected override void Repoint(DailyItemConsumption row, Guid primaryId) => row.ItemId = primaryId;

    protected override (DateOnly Date, Guid ClassId, Guid LocationId) KeyOf(DailyItemConsumption row) =>
        (row.Date, row.ClassId, row.LocationId);

    protected override void Combine(DailyItemConsumption survivor, DailyItemConsumption other)
    {
        survivor.Quantity += other.Quantity;
        survivor.TotalValue += other.TotalValue;
        if (other.ComputedAt > survivor.ComputedAt)
            survivor.ComputedAt = other.ComputedAt;
    }
}
