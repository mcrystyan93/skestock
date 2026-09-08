using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Features.Stock.Commands.AdjustStock;
using skestock.Application.Features.Stock.Models;
using skestock.Application.Features.Stock.Queries.GetClassLocationStock;
using static skestock.Application.Features.Stock.Models.StockRequests;

namespace skestock.Web.Endpoints;

public class Stock : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetClassLocationStock, "class/{classId}");
        groupBuilder.MapPost(AdjustStock, "adjust");
    }

    [EndpointSummary("Get current stock for a class with optional column filters")]
    [EndpointDescription("Retrieves the current stock (sum of remaining stock batch quantities) " +
                          "for every item received by a given school class, flagging items whose " +
                          "quantity is below their configured minimum threshold. The optional " +
                          "'locationId' and 'categoryId' equals filters scope the report, and the " +
                          "'searchTerm' property filters item names case-insensitively.")]
    public static async Task<Results<Ok<List<StockItemDto>>, ProblemHttpResult>> GetClassLocationStock(
        ISender sender, Guid classId, StockRequests.GetClassLocationStockRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetClassLocationStockQuery
        {
            ClassId = classId,
            Filters = request.Filters,
            SearchTerm = request.SearchTerm
        };

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Adjust stock for an item at a location after a physical recount")]
    [EndpointDescription("Reconciles the actual counted quantity against the current stock " +
                          "total (sum of remaining stock batch quantities) for a given class, " +
                          "item and location. A shortfall is drawn down from existing batches, " +
                          "oldest expiry first, writing one adjustment transaction per batch " +
                          "touched. A surplus creates a new, unattributed stock batch. Returns " +
                          "a validation error if the counted quantity matches the current total.")]
    public static async Task<Results<Ok<StockItemDto>, ProblemHttpResult>> AdjustStock(
        ISender sender, AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var command = new AdjustStockCommand
        {
            ClassId = request.ClassId,
            ItemId = request.ItemId,
            LocationId = request.LocationId,
            ActualQuantity = request.ActualQuantity,
            Reason = request.Reason
        };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }
}
