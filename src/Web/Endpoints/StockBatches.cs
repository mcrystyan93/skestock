using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.StockBatches.Models;
using skestock.Application.Features.StockBatches.Queries.GetAllStockBatches;

namespace skestock.Web.Endpoints;

public class StockBatches : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetAllStockBatches, "get-all");
    }

    [EndpointSummary("Get all stock batches")]
    [EndpointDescription("Retrieves a paginated, filterable list of stock batches. Filter by goodsReceiptId, itemId, locationId, etc. via the Filters column-filter list.")]
    public static async Task<Results<Ok<PaginatedResponse<StockBatchListItemDto>>, ProblemHttpResult>> GetAllStockBatches(
        ISender sender, StockBatchRequests.GetAllStockBatchesRequest request, CancellationToken cancellationToken)
    {
        var query = new GetAllStockBatchesQuery
        {
            Filters = request.Filters,
            Sort = request.Sort,
            Cursor = request.Cursor,
            SearchTerm = request.SearchTerm,
            PageSize = request.PageSize
        };

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }
}
