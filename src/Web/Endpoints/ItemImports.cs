using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Commands.ConfirmItemImport;
using skestock.Application.Features.Items.Commands.CreateItemImport;
using skestock.Application.Features.Items.Models;
using skestock.Application.Features.Items.Queries.GetAllItemImports;
using skestock.Application.Features.Items.Queries.GetItemImportById;

namespace skestock.Web.Endpoints;

public class ItemImports : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreateItemImport, "").RequireAuthorization();
        groupBuilder.MapPost(GetAllItemImports, "get-all").RequireAuthorization();
        groupBuilder.MapGet(GetItemImportById, "{id}").RequireAuthorization();
        groupBuilder.MapPost(ConfirmItemImport, "{id}/confirm").RequireAuthorization();
    }

    [EndpointSummary("Create a new item import")]
    [EndpointDescription(
        "Starts an item import for an uploaded file: creates an ItemImport in the Processing state, capturing the blob path from the referenced file metadata.")]
    public static async Task<Results<Created<ItemImportDto>, ProblemHttpResult>> CreateItemImport(
        ISender sender,
        ItemImportRequests.CreateItemImportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateItemImportCommand { FileMetadataId = request.FileMetadataId },
            cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Created($"/api/ItemImports/{result.Value.Id}", result.Value);
    }

    [EndpointSummary("Get all item imports")]
    [EndpointDescription("Retrieves a paginated, filterable list of item imports.")]
    public static async Task<Results<Ok<PaginatedResponse<ItemImportListItemDto>>, ProblemHttpResult>>
        GetAllItemImports(
            ISender sender,
            ItemImportRequests.GetAllItemImportsRequest request,
            CancellationToken cancellationToken)
    {
        var query = new GetAllItemImportsQuery
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

    [EndpointSummary("Get an item import for review")]
    [EndpointDescription("Retrieves an item import's extracted item suggestions for review.")]
    public static async Task<Results<Ok<ItemImportReviewDto>, ProblemHttpResult>> GetItemImportById(
        ISender sender,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetItemImportByIdQuery { Id = id }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Confirm an item import")]
    [EndpointDescription(
        "Confirms a reviewed item import using the explicitly selected catalog items.")]
    public static async Task<Results<Ok<ItemImportConfirmationResultDto>, ProblemHttpResult>> ConfirmItemImport(
        ISender sender,
        Guid id,
        ItemImportRequests.ConfirmItemImportRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmItemImportCommand
        {
            ImportId = id,
            Items = request.Items.Select(i => new ConfirmItemImportItem
            {
                ItemId = i.ItemId,
                Sku = i.Sku,
                Name = i.Name,
                Unit = i.Unit,
                Description = i.Description,
                IsPerishable = i.IsPerishable
            }).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }
}
