using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Features.Stock.Commands.AdjustStock;
using skestock.Application.Features.Stock.Commands.MoveStock;
using skestock.Application.Features.Stock.Commands.RemoveExpiredStock;
using skestock.Application.Features.Stock.Commands.SetClassItemStockVisibility;
using skestock.Application.Features.Stock.Models;
using skestock.Application.Features.Stock.Queries.GetClassLocationStock;
using static skestock.Application.Features.Stock.Models.StockRequests;

namespace skestock.Web.Endpoints;

public class Stock : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetClassLocationStock, "class/{classId}");
        groupBuilder.MapGet(GetLowStockItems, "class/{classId}/low-stock");
        groupBuilder.MapPost(AdjustStock, "adjust");
        groupBuilder.MapPost(RemoveExpiredStock, "remove-expired");
        groupBuilder.MapPost(MoveStock, "move");
        groupBuilder.MapPatch(
            SetClassItemStockVisibility,
            "class/{classId}/item/{itemId}/location/{locationId}/visibility");
    }

    [EndpointSummary("Get current stock for a class with optional column filters")]
    [EndpointDescription("Retrieves the current stock (sum of remaining stock batch quantities) " +
                          "for every item received by a given school class, flagging items whose " +
                          "quantity is below their configured minimum threshold or may contain " +
                          "expired perishable batches. The response also exposes whether any " +
                          "visible item may be expired. The optional 'locationId' and 'categoryId' " +
                          "equals filters scope the report, and the 'searchTerm' property filters " +
                          "item names case-insensitively. The 'lowStockOnly' and 'expiredOnly' " +
                          "flags restrict the report to matching rows when enabled.")]
    public static async Task<Results<Ok<StockReportDto>, ProblemHttpResult>> GetClassLocationStock(
        ISender sender, Guid classId, StockRequests.GetClassLocationStockRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetClassLocationStockQuery
        {
            ClassId = classId,
            Filters = request.Filters,
            SearchTerm = request.SearchTerm,
            IncludeHidden = request.IncludeHidden,
            LowStockOnly = request.LowStockOnly,
            ExpiredOnly = request.ExpiredOnly
        };

        var result = await sender.Send(query, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Get low-stock items for a class")]
    [EndpointDescription("Retrieves items whose current quantity is below their configured " +
                          "minimum threshold, including the item name, SKU, unit and storage " +
                          "location.")]
    public static async Task<Results<Ok<List<LowStockItemDto>>, ProblemHttpResult>> GetLowStockItems(
        ISender sender, Guid classId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetClassLocationStockQuery
            {
                ClassId = classId,
                LowStockOnly = true
            },
            cancellationToken);

        return result.ToOk(report => report.Items
            .Select(item => new LowStockItemDto
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Sku = item.Sku,
                Unit = item.Unit,
                LocationName = item.LocationName
            })
            .ToList());
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

        return result.ToOk();
    }

    [EndpointSummary("Remove expired stock for an item at a location")]
    [EndpointDescription("Removes only the remaining quantity from expired perishable stock batches " +
                          "for the requested class, item and location. Each affected batch is audited " +
                          "as an expired stock adjustment, and the stock-adjusted realtime event is emitted.")]
    public static async Task<Results<Ok, ProblemHttpResult>> RemoveExpiredStock(
        ISender sender, RemoveExpiredStockRequest request, CancellationToken cancellationToken)
    {
        var command = new RemoveExpiredStockCommand
        {
            ClassId = request.ClassId,
            ItemId = request.ItemId,
            LocationId = request.LocationId
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Move stock between two locations")]
    [EndpointDescription("Moves a positive quantity of an item from one location to another within " +
                         "the same school-class stock scope. Source batches are consumed oldest " +
                         "expiry first and their metadata is preserved in distinct destination " +
                         "batches. Returns an empty successful response.")]
    public static async Task<Results<Ok, ProblemHttpResult>> MoveStock(
        ISender sender, MoveStockRequest request, CancellationToken cancellationToken)
    {
        var command = new MoveStockCommand
        {
            ClassId = request.ClassId,
            ItemId = request.ItemId,
            SourceLocationId = request.SourceLocationId,
            DestinationLocationId = request.DestinationLocationId,
            Quantity = request.Quantity
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Change zero-stock visibility for an item in a class")]
    [EndpointDescription("Stores whether the selected item should be hidden from the normal class " +
                         "stock report for the selected location after its quantity reaches zero.")]
    public static async Task<Results<Ok, ProblemHttpResult>> SetClassItemStockVisibility(
        ISender sender,
        Guid classId,
        Guid itemId,
        Guid locationId,
        SetClassItemStockVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetClassItemStockVisibilityCommand
        {
            ClassId = classId,
            ItemId = itemId,
            LocationId = locationId,
            HideWhenZeroStock = request.HideWhenZeroStock
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToOk();
    }
}
