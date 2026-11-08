using skestock.Application.Common.Models;
using skestock.Application.Common.Security;
using skestock.Domain.Constants;

namespace skestock.Application.Features.EntityHistory.Queries.GetEntityHistory;

// Not cacheable: no command could invalidate it generically, and stale audit data is wrong data.
[Authorize(Roles = Roles.Administrator)]
public sealed class GetEntityHistoryQuery : IRequest<Result<PaginatedResponse<EntityHistoryVersionDto>>>
{
    public string Entity { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;

    /// <summary>Offset of the next page, as returned in <c>NextCursor</c>.</summary>
    public string? Cursor { get; init; }

    public int PageSize { get; init; } = PaginationConstants.DEFAULT_PAGE_SIZE;
}
