using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Features.Statistics.Models;
using skestock.Application.Features.Statistics.Queries.GetClassGoodsReceiptCosts;
using skestock.Application.Features.Statistics.Queries.GetClassItemStockEvolution;
using skestock.Application.Features.Statistics.Queries.GetClassLocationStockByItem;
using skestock.Application.Features.Statistics.Queries.GetClassDailyConsumption;
using skestock.Application.Features.Statistics.Queries.GetClassStockByCategory;
using skestock.Application.Features.Statistics.Queries.GetDailyConsumptionAverages;
using skestock.Application.Features.Statistics.Queries.GetItemsPurchaseHistory;
using skestock.Application.Features.Statistics.Queries.GetTopPurchases;
using skestock.Domain.Enums;

namespace skestock.Web.Endpoints;

public class Statistics : IEndpointGroup
{
    public static string? RoutePrefix => "/api/statistics";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetClassStockByCategory, "class/{classId}/stock-by-category");
        groupBuilder.MapGet(
            GetClassLocationStockByItem,
            "class/{classId}/location/{locationId}/stock-by-item");
        groupBuilder.MapGet(GetClassGoodsReceiptCosts, "class/{classId}/goods-receipt-costs");
        groupBuilder.MapGet(
            GetClassItemStockEvolution,
            "class/{classId}/item/{itemId}/stock-evolution");
        groupBuilder.MapGet(GetDailyConsumptionAverages, "daily-consumption/averages");
        groupBuilder.MapGet(GetClassDailyConsumption, "class/{classId}/daily-consumption");
        groupBuilder.MapGet(GetTopPurchases, "purchases/top");
        groupBuilder.MapPost(GetItemsPurchaseHistory, "purchases/items-history");
    }

    [EndpointSummary("Get current stock grouped by category")]
    [EndpointDescription("Returns current stock for a class, grouped into category series across " +
                         "locations with stock batches. Labels are location names and labelIds the matching " +
                         "location ids. The response is suitable for a stacked bar chart.")]
    public static async Task<Results<Ok<ClassStockByCategoryDto>, ProblemHttpResult>> GetClassStockByCategory(
        ISender sender,
        Guid classId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetClassStockByCategoryQuery { ClassId = classId }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Get current stock by item for a location")]
    [EndpointDescription("Returns current non-zero stock for each item a school class holds at one " +
                         "location. Labels are item names and labelIds the matching item ids; each " +
                         "item's quantity sits in its category series, so a stacked bar chart is " +
                         "coloured by category.")]
    public static async Task<Results<Ok<ClassStockByCategoryDto>, ProblemHttpResult>>
        GetClassLocationStockByItem(
            ISender sender,
            Guid classId,
            Guid locationId,
            CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetClassLocationStockByItemQuery { ClassId = classId, LocationId = locationId },
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
                         "of its first stock transaction, plus totals and the overall daily average. " +
                         "Optionally filtered by item, location and category.")]
    public static async Task<Results<Ok<ClassDailyConsumptionDto>, ProblemHttpResult>>
        GetClassDailyConsumption(
            ISender sender,
            Guid classId,
            [AsParameters] StatisticsRequests.GetClassDailyConsumptionRequest request,
            CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetClassDailyConsumptionQuery
            {
                ClassId = classId,
                ItemId = request.ItemId,
                LocationId = request.LocationId,
                CategoryId = request.CategoryId
            },
            cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Get top purchased items")]
    [EndpointDescription("Returns the items ranked by purchased quantity, purchase value and purchase frequency " +
                         "for a scope (Last90Days, Last365Days, or Class with classId), optionally filtered by " +
                         "category. Figures come from the daily purchase statistics job.")]
    public static async Task<Results<Ok<TopPurchasesDto>, ProblemHttpResult>>
        GetTopPurchases(
            ISender sender,
            [AsParameters] StatisticsRequests.GetTopPurchasesRequest request,
            CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetTopPurchasesQuery
            {
                Scope = request.Scope ?? PurchaseStatisticsScope.Last365Days,
                ClassId = request.ClassId,
                CategoryId = request.CategoryId,
                Top = request.Top ?? GetTopPurchasesQuery.DefaultTop
            },
            cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Get purchase history for items")]
    [EndpointDescription("Returns the last-365-days purchase statistics (frequency, average quantity, last " +
                         "purchase) for the requested items. Items without purchases are omitted.")]
    public static async Task<Results<Ok<List<PurchaseStatisticDto>>, ProblemHttpResult>>
        GetItemsPurchaseHistory(
            ISender sender,
            StatisticsRequests.GetItemsPurchaseHistoryRequest request,
            CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetItemsPurchaseHistoryQuery { ItemIds = request.ItemIds },
            cancellationToken);

        return result.ToOk();
    }
}
