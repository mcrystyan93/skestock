using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.EntityHistory.Queries.GetEntityHistory;

public sealed class GetEntityHistoryHandler(IEntityHistoryReader reader)
    : IRequestHandler<GetEntityHistoryQuery, Result<PaginatedResponse<EntityHistoryVersionDto>>>
{
    public async ValueTask<Result<PaginatedResponse<EntityHistoryVersionDto>>> Handle(
        GetEntityHistoryQuery request, CancellationToken cancellationToken)
    {
        // Offset paging on purpose: history rows have no unique key, so a keyset cursor could skip tied rows. The reader
        // orders by every column after the period, so tied rows are identical and offsets stay stable between pages.
        var skip = string.IsNullOrEmpty(request.Cursor) ? 0 : int.Parse(request.Cursor);

        var result = await reader.ReadAsync(request.Entity, request.Id, skip, request.PageSize, cancellationToken);
        if (result.IsFailed)
            return result.ToResult();

        var page = result.Value;
        return Result.Ok(new PaginatedResponse<EntityHistoryVersionDto>
        {
            Data = page.Versions,
            HasNextPage = page.HasNextPage,
            NextCursor = page.HasNextPage ? (skip + page.Versions.Count).ToString() : null,
            Sort = []
        });
    }
}
