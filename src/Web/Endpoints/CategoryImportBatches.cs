using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.Categories.Commands.ConfirmCategoryImportBatch;
using skestock.Application.Features.Categories.Commands.CreateCategoryImportBatch;
using skestock.Application.Features.Categories.Models;
using skestock.Application.Features.Categories.Queries.GetAllCategoryImportBatches;
using skestock.Application.Features.Categories.Queries.GetCategoryImportBatchById;

namespace skestock.Web.Endpoints;

public class CategoryImportBatches : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreateCategoryImportBatch, "");
        groupBuilder.MapPost(GetAllCategoryImportBatches, "get-all");
        groupBuilder.MapGet(GetCategoryImportBatchById, "{id}");
        groupBuilder.MapPost(ConfirmCategoryImportBatch, "{id}/confirm");
    }

    [EndpointSummary("Create a new category import")]
    [EndpointDescription("Starts a category import batch for previously uploaded files.")]
    public static async Task<Results<Created<CategoryImportBatchMutationDto>, ProblemHttpResult>> CreateCategoryImportBatch(
        ISender sender,
        CategoryImportBatchRequests.CreateCategoryImportBatchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCategoryImportBatchCommand
            {
                FileMetadataIds = request.FileMetadataIds,
                ClientRequestId = request.ClientRequestId
            },
            cancellationToken);

        return result.ToCreated(v => $"/api/CategoryImportBatches/{v.Id}");
    }

    [EndpointSummary("Get all category imports")]
    [EndpointDescription("Retrieves a paginated, filterable list of category imports.")]
    public static async Task<Results<Ok<PaginatedResponse<CategoryImportBatchListItemDto>>, ProblemHttpResult>> GetAllCategoryImportBatches(
        ISender sender,
        CategoryImportBatchRequests.GetAllCategoryImportBatchesRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetAllCategoryImportBatchesQuery
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

    [EndpointSummary("Get a category import for review")]
    [EndpointDescription("Retrieves a category import's extracted category-name suggestions for review.")]
    public static async Task<Results<Ok<CategoryImportBatchReviewDto>, ProblemHttpResult>> GetCategoryImportBatchById(
        ISender sender,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCategoryImportBatchByIdQuery { Id = id }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Confirm a category import")]
    [EndpointDescription("Confirms a reviewed category import using the explicit reviewed names.")]
    public static async Task<Results<Ok<CategoryImportBatchConfirmationResponseDto>, ProblemHttpResult>> ConfirmCategoryImportBatch(
        ISender sender,
        Guid id,
        CategoryImportBatchRequests.ConfirmCategoryImportBatchRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmCategoryImportBatchCommand
        {
            BatchId = id,
            CategoryNames = request.Names
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToOk();
    }
}
