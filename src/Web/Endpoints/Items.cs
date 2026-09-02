using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Commands.CreateItem;
using skestock.Application.Features.Items.Commands.DisableItem;
using skestock.Application.Features.Items.Commands.EditItem;
using skestock.Application.Features.Items.Commands.EnableItem;
using skestock.Application.Features.Items.Models;
using skestock.Application.Features.Items.Queries.GetAllItems;
using skestock.Application.Features.Items.Queries.GetItemById;

namespace skestock.Web.Endpoints;

public class Items : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetAllItems, "get-all");
        groupBuilder.MapGet(GetItemById, "{id}");
        groupBuilder.MapPost(CreateItem, "");
        groupBuilder.MapPut(EditItem, "{id}");
        groupBuilder.MapPatch(EnableItem, "{id}/enable");
        groupBuilder.MapPatch(DisableItem, "{id}/disable");
    }

    [EndpointSummary("Get all items")]
    [EndpointDescription("Retrieves all items from the database.")]
    public static async Task<Results<Ok<PaginatedResponse<ItemDto>>, ProblemHttpResult>> GetAllItems(
        ISender sender, ItemRequests.GetAllItemsRequest request, CancellationToken cancellationToken)
    {
        var query = new GetAllItemsQuery
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

    [EndpointSummary("Get an item by id")]
    [EndpointDescription("Retrieves a single item by its id.")]
    public static async Task<Results<Ok<ItemDto>, ProblemHttpResult>> GetItemById(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetItemByIdQuery { Id = id }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Create a new item")]
    [EndpointDescription("Creates a new item in the database.")]
    public static async Task<Results<Created<ItemDto>, ProblemHttpResult>> CreateItem(
        ISender sender, ItemRequests.CreateItemRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateItemCommand
        {
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            Unit = request.Unit,
            MinThreshold = request.MinThreshold,
            IsPerishable = request.IsPerishable,
            CategoryId = request.CategoryId
        };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Created($"/items/{result.Value.Id}", result.Value);
    }

    [EndpointSummary("Edit an existing item")]
    [EndpointDescription("Updates an existing item's details in the database.")]
    public static async Task<Results<Ok<ItemDto>, ProblemHttpResult>> EditItem(
        ISender sender, Guid id, ItemRequests.EditItemRequest request, CancellationToken cancellationToken)
    {
        var command = new EditItemCommand
        {
            Id = id,
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            Unit = request.Unit,
            MinThreshold = request.MinThreshold,
            IsPerishable = request.IsPerishable,
            CategoryId = request.CategoryId
        };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Enable an item")]
    [EndpointDescription("Marks an existing item as active.")]
    public static async Task<Results<Ok<ItemDto>, ProblemHttpResult>> EnableItem(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new EnableItemCommand { Id = id }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Disable an item")]
    [EndpointDescription("Marks an existing item as inactive.")]
    public static async Task<Results<Ok<ItemDto>, ProblemHttpResult>> DisableItem(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DisableItemCommand { Id = id }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }
}
