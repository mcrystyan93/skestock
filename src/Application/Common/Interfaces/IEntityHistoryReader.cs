using skestock.Application.Features.EntityHistory.Queries.GetEntityHistory;

namespace skestock.Application.Common.Interfaces;

public interface IEntityHistoryReader
{
    /// <summary>
    /// Reads versions of one row of a temporal table, newest first. <paramref name="entity"/> is a table name and must
    /// match the allowlist built from the EF model; unknown tables yield <c>UnknownEntity</c>, unparsable ids
    /// <c>InvalidEntityId</c>.
    /// </summary>
    Task<Result<EntityHistoryPage>> ReadAsync(string entity, string id, int skip, int take,
        CancellationToken cancellationToken);
}

public sealed record EntityHistoryPage(IReadOnlyList<EntityHistoryVersionDto> Versions, bool HasNextPage);
