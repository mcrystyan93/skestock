using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Congregations;

internal static class CongregationNameValidation
{
    // excludeId is the congregation being updated (null on create) so its own name isn't a duplicate.
    // Soft-deleted rows are excluded by the entity's global query filter.
    public static async Task<bool> IsNameTakenAsync(
        IApplicationDbContext dbContext, Guid? excludeId, string name, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        var query = dbContext.Congregations.AsNoTracking().AsQueryable();
        query = TextSearchCollation.IsSqlServer(dbContext.Database)
            ? query.Where(c => c.Id != excludeId &&
                EF.Functions.Collate(c.Name.Trim(), TextSearchCollation.AccentInsensitive) == normalized)
            : query.Where(c => c.Id != excludeId && c.Name.Trim().ToLower() == normalized);

        return await query.AnyAsync(cancellationToken);
    }
}
