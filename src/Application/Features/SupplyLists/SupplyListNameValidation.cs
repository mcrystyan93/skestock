using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.SupplyLists;

internal static class SupplyListNameValidation
{
    // excludeId is the list being updated (null on create) so its own name isn't a duplicate.
    public static async Task<bool> IsNameTakenAsync(
        IApplicationDbContext dbContext, Guid? excludeId, string name, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        var query = dbContext.SupplyLists.AsNoTracking().AsQueryable();
        query = TextSearchCollation.IsSqlServer(dbContext.Database)
            ? query.Where(l => l.Id != excludeId &&
                EF.Functions.Collate(l.Name.Trim(), TextSearchCollation.AccentInsensitive) == normalized)
            : query.Where(l => l.Id != excludeId && l.Name.Trim().ToLower() == normalized);

        return await query.AnyAsync(cancellationToken);
    }
}
