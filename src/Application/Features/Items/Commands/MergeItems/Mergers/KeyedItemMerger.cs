using skestock.Domain.Common;

namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

/// <summary>
/// Base for tables with a unique key that includes the item. Rows of the primary and all secondaries are grouped
/// by that key; per group the primary's row survives (else the first secondary row, re-pointed) and the rest are
/// <see cref="Combine"/>d into it and deleted. Preview and merge share <see cref="Plan"/>, so counts cannot drift.
/// </summary>
public abstract class KeyedItemMerger<T, TKey>(ItemMergeKind kind) : IItemReferenceMerger
    where T : BaseEntity
    where TKey : notnull
{
    public ItemMergeKind Kind => kind;

    protected abstract DbSet<T> Rows { get; }

    protected abstract Guid ItemIdOf(T row);

    protected abstract void Repoint(T row, Guid primaryId);

    // The key without the item, e.g. (ClassId, LocationId).
    protected abstract TKey KeyOf(T row);

    /// <summary>Folds a colliding row's values into the survivor. Default: keep the survivor as is.</summary>
    protected virtual void Combine(T survivor, T other) { }

    private IQueryable<T> RowsOf(IReadOnlyCollection<Guid> itemIds) =>
        Rows.Where(row => itemIds.Contains(EF.Property<Guid>(row, "ItemId")));

    private IEnumerable<(T Survivor, IReadOnlyList<T> Others)> Plan(IEnumerable<T> rows, Guid primaryId) =>
        rows.GroupBy(KeyOf).Select(group =>
        {
            var ordered = group.OrderByDescending(row => ItemIdOf(row) == primaryId).ThenBy(row => row.Id).ToList();
            return (ordered[0], (IReadOnlyList<T>)ordered.Skip(1).ToList());
        });

    private static IReadOnlyCollection<Guid> AllIds(Guid primaryId, IReadOnlyCollection<Guid> secondaryIds) =>
        [primaryId, .. secondaryIds];

    public async Task<ItemMergeImpact> PreviewAsync(
        Guid primaryId, IReadOnlyCollection<Guid> secondaryIds, CancellationToken cancellationToken)
    {
        var rows = await RowsOf(AllIds(primaryId, secondaryIds)).AsNoTracking().ToListAsync(cancellationToken);
        var plan = Plan(rows, primaryId).ToList();
        return new ItemMergeImpact(kind, plan.Count(p => ItemIdOf(p.Survivor) != primaryId), plan.Sum(p => p.Others.Count));
    }

    public async Task MergeAsync(
        Guid primaryId, IReadOnlyCollection<Guid> secondaryIds, CancellationToken cancellationToken)
    {
        var rows = await RowsOf(AllIds(primaryId, secondaryIds)).ToListAsync(cancellationToken);
        foreach (var (survivor, others) in Plan(rows, primaryId))
        {
            foreach (var other in others)
            {
                Combine(survivor, other);
                Rows.Remove(other);
            }

            if (ItemIdOf(survivor) != primaryId)
                Repoint(survivor, primaryId);
        }
    }
}
