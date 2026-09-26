using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Features.Statistics.Models;
using skestock.Application.Features.Statistics.Queries.GetClassGoodsReceiptCosts;
using skestock.Application.Features.Statistics.Queries.GetClassItemStockEvolution;
using skestock.Application.Features.Statistics.Queries.GetClassDailyConsumption;
using skestock.Application.Features.Statistics.Queries.GetClassStockByCategory;
using skestock.Application.Features.Statistics.Queries.GetDailyConsumptionAverages;

namespace skestock.Web.Endpoints;

public class Statistics : IEndpointGroup
{
    public static string? RoutePrefix => "/api/statistics";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetClassStockByCategory, "class/{classId}/stock-by-category");
        groupBuilder.MapGet(
            GetClassStockByCategoryForLocation,
            "class/{classId}/location/{locationId}/stock-by-category");
        groupBuilder.MapGet(GetClassGoodsReceiptCosts, "class/{classId}/goods-receipt-costs");
        groupBuilder.MapGet(
            GetClassItemStockEvolution,
            "class/{classId}/item/{itemId}/stock-evolution");
        groupBuilder.MapGet(GetDailyConsumptionAverages, "daily-consumption/averages");
        groupBuilder.MapGet(GetClassDailyConsumption, "class/{classId}/daily-consumption");
    }

    [EndpointSummary("Get current stock grouped by category")]
    [EndpointDescription("Returns current stock for a class, grouped into category series across " +
                         "locations with stock batches. The response is suitable for a stacked bar chart.")]
    public static async Task<Results<Ok<ClassStockByCategoryDto>, ProblemHttpResult>> GetClassStockByCategory(
        ISender sender,
        Guid classId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetClassStockByCategoryQuery { ClassId = classId }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Get current stock by category for a location")]
    [EndpointDescription("Returns current stock for one location in a school class, with category " +
                         "series zero-filled for categories stocked elsewhere in the class.")]
    public static async Task<Results<Ok<ClassStockByCategoryDto>, ProblemHttpResult>>
        GetClassStockByCategoryForLocation(
            ISender sender,
            Guid classId,
            Guid locationId,
            CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetClassStockByCategoryQuery { ClassId = classId, LocationId = locationId },
            cancellationToken);

        return result.ToOk();
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

        return result.ToOk();
    }

    [EndpointSummary("Get an item's stock evolution for a class")]
    [EndpointDescription("Returns the cumulative, class-wide stock level for an item, starting with its first " +
                         "transaction in the class and grouping changes by UTC calendar day.")]
    public static async Task<Results<Ok<ClassItemStockEvolutionDto>, ProblemHttpResult>>
        GetClassItemStockEvolution(
            ISender sender,
            Guid classId,
            Guid itemId,
            CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetClassItemStockEvolutionQuery { ClassId = classId, ItemId = itemId },
            cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Get average daily consumption")]
    [EndpointDescription("Returns the average daily consumption (quantity and RON value) over the last 7 and " +
                         "30 complete local days, optionally filtered by item, location and category.")]
    public static async Task<Results<Ok<DailyConsumptionAveragesDto>, ProblemHttpResult>>
        GetDailyConsumptionAverages(
            ISender sender,
            [AsParameters] StatisticsRequests.GetDailyConsumptionAveragesRequest request,
            CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetDailyConsumptionAveragesQuery
            {
                ItemId = request.ItemId,
                LocationId = request.LocationId,
                CategoryId = request.CategoryId
            },
            cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Get a class's daily consumption series")]
    [EndpointDescription("Returns zero-filled daily consumption points for a class, starting with the local date " +
                         "of its first stock transaction, plus totals and the overall daily average.")]
    public static async Task<Results<Ok<ClassDailyConsumptionDto>, ProblemHttpResult>>
        GetClassDailyConsumption(
            ISender sender,
            Guid classId,
            CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetClassDailyConsumptionQuery { ClassId = classId },
            cancellationToken);

        return result.ToOk();
    }
}
