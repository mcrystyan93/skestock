namespace skestock.Application.Features.Items.Commands.MergeItems.Mergers;

/// <summary>Base for tables with no unique key on the item: every secondary row is re-pointed.</summary>
public abstract class PlainItemMerger<T>(ItemMergeKind kind) : IItemReferenceMerger
{
    public ItemMergeKind Kind => kind;

    protected abstract IQueryable<T> RowsOf(IReadOnlyCollection<Guid> itemIds);

    protected abstract void Repoint(T row, Guid primaryId);

    public async Task<ItemMergeImpact> PreviewAsync(
        Guid primaryId, IReadOnlyCollection<Guid> secondaryIds, CancellationToken cancellationToken) =>
        new(kind, await RowsOf(secondaryIds).CountAsync(cancellationToken), 0);

    public async Task MergeAsync(
        Guid primaryId, IReadOnlyCollection<Guid> secondaryIds, CancellationToken cancellationToken)
    {
        foreach (var row in await RowsOf(secondaryIds).ToListAsync(cancellationToken))
            Repoint(row, primaryId);
    }
}
