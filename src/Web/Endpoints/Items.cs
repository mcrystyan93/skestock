using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Commands.CreateItem;
using skestock.Application.Features.Items.Commands.DisableItem;
using skestock.Application.Features.Items.Commands.EditItem;
using skestock.Application.Features.Items.Commands.EnableItem;
using skestock.Application.Features.Items.Models;
using skestock.Application.Features.Items.Queries.GetAllItems;
using skestock.Application.Features.Items.Queries.GetItemById;
using skestock.Application.Features.Items.Queries.GetItemImage;
using Microsoft.Net.Http.Headers;

namespace skestock.Web.Endpoints;

public class Items : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetAllItems, "get-all");
        groupBuilder.MapGet(GetItemById, "{id}");
        groupBuilder.MapGet(GetItemImage, "{id}/image");
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

        return result.ToOk();
    }

    [EndpointSummary("Get an item by id")]
    [EndpointDescription("Retrieves a single item by its id.")]
    public static async Task<Results<Ok<ItemDto>, ProblemHttpResult>> GetItemById(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetItemByIdQuery { Id = id }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Get an item's image")]
    [EndpointDescription("Streams the item's image: size=thumb (WebP thumbnail, falls back to the original until generated) or size=full. Use ?v=<imageFileId> to bust caches.")]
    public static async Task<Results<FileStreamHttpResult, ProblemHttpResult>> GetItemImage(
        ISender sender, HttpContext httpContext, Guid id, CancellationToken cancellationToken,
        string? size = null)
    {
        // Minimal-API enum binding is case-sensitive; the documented contract is lowercase thumb|full.
        var parsedSize = ItemImageSize.Thumb;
        if (size is not null && (!Enum.TryParse(size, ignoreCase: true, out parsedSize) || !Enum.IsDefined(parsedSize)))
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid image size", detail: "size must be 'thumb' or 'full'.");

        var result = await sender.Send(new GetItemImageQuery(id, parsedSize), cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        var image = result.Value;
        var headers = httpContext.Response.Headers;
        // The thumbnail is served from the original until the Worker produces it, so the fallback must not be pinned.
        headers.CacheControl = image.IsFallback ? "no-cache" : "private, max-age=31536000, immutable";
        headers[HeaderNames.XContentTypeOptions] = "nosniff";

        return TypedResults.Stream(image.Content, image.ContentType);
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
            ShelfLifeDays = request.ShelfLifeDays,
            CategoryId = request.CategoryId,
            ImageFileId = request.ImageFileId
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToCreated(v => $"/items/{v.Id}");
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
            ShelfLifeDays = request.ShelfLifeDays,
            CategoryId = request.CategoryId,
            ImageFileId = request.ImageFileId
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Enable an item")]
    [EndpointDescription("Marks an existing item as active.")]
    public static async Task<Results<Ok<ItemDto>, ProblemHttpResult>> EnableItem(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new EnableItemCommand { Id = id }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Disable an item")]
    [EndpointDescription("Marks an existing item as inactive.")]
    public static async Task<Results<Ok<ItemDto>, ProblemHttpResult>> DisableItem(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DisableItemCommand { Id = id }, cancellationToken);

        return result.ToOk();
    }
}
