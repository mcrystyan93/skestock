using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Features.Statistics.Models;
using skestock.Application.Features.Statistics.Queries.GetClassGoodsReceiptCosts;
using skestock.Application.Features.Statistics.Queries.GetClassStockByCategory;

namespace skestock.Web.Endpoints;

public class Statistics : IEndpointGroup
{
    public static string? RoutePrefix => "/api/statistics";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetClassStockByCategory, "class/{classId}/stock-by-category");
        groupBuilder.MapGet(GetClassGoodsReceiptCosts, "class/{classId}/goods-receipt-costs");
    }

    [EndpointSummary("Get current stock grouped by category")]
    [EndpointDescription("Returns positive on-hand stock for a class, grouped by category. " +
                         "All locations are included unless a locationId query parameter is supplied.")]
    public static async Task<Results<Ok<ClassStockByCategoryDto>, ProblemHttpResult>> GetClassStockByCategory(
        ISender sender,
        Guid classId,
        [AsParameters] StatisticsRequests.GetClassStockByCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetClassStockByCategoryQuery
        {
            ClassId = classId,
            LocationId = request.LocationId
        }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Get goods receipt cost points")]
    [EndpointDescription("Returns stored goods receipt costs for a class, optionally filtered by " +
                         "an inclusive received date range.")]
    public static async Task<Results<Ok<ClassGoodsReceiptCostsDto>, ProblemHttpResult>> GetClassGoodsReceiptCosts(
        ISender sender,
        Guid classId,
        [AsParameters] StatisticsRequests.GetClassGoodsReceiptCostsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetClassGoodsReceiptCostsQuery
        {
            ClassId = classId,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }
}
