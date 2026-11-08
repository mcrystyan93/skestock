using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.EntityHistory.Queries.GetEntityHistory;

namespace skestock.Web.Endpoints;

public class EntityHistory : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetEntityHistory, "{entity}/{id}");
    }

    [EndpointSummary("Get the history of a row")]
    [EndpointDescription("Administrators only. Lists the retained versions of one row of a temporal table, newest first, with the user who wrote each version. The entity is a table name such as Items or StockBatches.")]
    public static async Task<Results<Ok<PaginatedResponse<EntityHistoryVersionDto>>, ProblemHttpResult>> GetEntityHistory(
        ISender sender, string entity, string id, string? cursor, int? pageSize, CancellationToken cancellationToken)
    {
        var query = new GetEntityHistoryQuery
        {
            Entity = entity,
            Id = id,
            Cursor = cursor,
            PageSize = pageSize ?? PaginationConstants.DEFAULT_PAGE_SIZE
        };

        var result = await sender.Send(query, cancellationToken);

        return result.ToOk();
    }
}
