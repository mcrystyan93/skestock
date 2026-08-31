using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Features.Stock.Models;
using skestock.Application.Features.Stock.Queries.GetClassLocationStock;

namespace skestock.Web.Endpoints;

public class Stock : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetClassLocationStock, "class/{classId}/location/{locationId}");
    }

    [EndpointSummary("Get current stock for a class and location")]
    [EndpointDescription("Retrieves the current stock (sum of remaining stock batch quantities) " +
                          "for every item received by a given school class at a given location, " +
                          "flagging items whose quantity is below their configured minimum threshold.")]
    public static async Task<Results<Ok<List<StockItemDto>>, ProblemHttpResult>> GetClassLocationStock(
        ISender sender, int classId, int locationId, CancellationToken cancellationToken)
    {
        var query = new GetClassLocationStockQuery { ClassId = classId, LocationId = locationId };

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }
}
