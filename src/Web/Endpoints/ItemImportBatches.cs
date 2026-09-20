using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Commands.ConfirmItemImportBatch;
using skestock.Application.Features.Items.Commands.CreateItemImportBatch;
using skestock.Application.Features.Items.Models;
using skestock.Application.Features.Items.Queries.GetAllItemImportBatches;
using skestock.Application.Features.Items.Queries.GetItemImportBatchById;

namespace skestock.Web.Endpoints;

public class ItemImportBatches : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreateItemImportBatch, "");
        groupBuilder.MapPost(GetAllItemImportBatches, "get-all");
        groupBuilder.MapGet(GetItemImportBatchById, "{id}");
        groupBuilder.MapPost(ConfirmItemImportBatch, "{id}/confirm");
    }

    [EndpointSummary("List item import batches")]
    [EndpointDescription("Returns a filtered, sorted, cursor-paginated list of item import batches for the Items table.")]
    public static async Task<Results<Ok<PaginatedResponse<ItemImportBatchListItemDto>>, ProblemHttpResult>>
        GetAllItemImportBatches(
            ISender sender,
            ItemImportBatchRequests.GetAllItemImportBatchesRequest request,
            CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllItemImportBatchesQuery
        {
            Filters = request.Filters,
            SearchTerm = request.SearchTerm,
            PageSize = request.PageSize,
            Cursor = request.Cursor,
            Sort = request.Sort
        }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Create a new item import batch")]
    [EndpointDescription("Starts an aggregate item import for several previously-uploaded files: creates an ItemImportBatch in the Processing state and enqueues one merged AI extraction covering every file.")]
    public static async Task<Results<Created<ItemImportBatchDto>, ProblemHttpResult>> CreateItemImportBatch(
        ISender sender,
        ItemImportBatchRequests.CreateItemImportBatchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateItemImportBatchCommand
            {
                FileMetadataIds = request.FileMetadataIds,
                ClientRequestId = request.ClientRequestId
            },
            cancellationToken);

        return result.ToCreated(v => $"/api/ItemImportBatches/{v.Id}");
    }

    [EndpointSummary("Get an item import batch for review")]
    [EndpointDescription("Retrieves an item import batch's merged extracted item suggestions for review, alongside the files that make up the batch.")]
    public static async Task<Results<Ok<ItemImportBatchReviewDto>, ProblemHttpResult>> GetItemImportBatchById(
        ISender sender,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetItemImportBatchByIdQuery { Id = id }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Confirm an item import batch")]
    [EndpointDescription("Confirms a reviewed item import batch using the explicit reviewed item selections.")]
    public static async Task<Results<Ok<ItemImportBatchConfirmationResultDto>, ProblemHttpResult>> ConfirmItemImportBatch(
        ISender sender,
        Guid id,
        ItemImportBatchRequests.ConfirmItemImportBatchRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmItemImportBatchCommand
        {
            BatchId = id,
            Items = request.Items.Select(item => new ConfirmItemImportBatchItem
            {
                ItemId = item.ItemId,
                Sku = item.Sku,
                Name = item.Name,
                Unit = item.Unit,
                Description = item.Description,
                IsPerishable = item.IsPerishable
            }).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToOk();
    }
}
