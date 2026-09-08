using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.Categories.Commands.ConfirmCategoryImport;
using skestock.Application.Features.Categories.Commands.CreateCategoryImport;
using skestock.Application.Features.Categories.Models;
using skestock.Application.Features.Categories.Queries.GetAllCategoryImports;
using skestock.Application.Features.Categories.Queries.GetCategoryImportById;

namespace skestock.Web.Endpoints;

public class CategoryImports : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreateCategoryImport, "").RequireAuthorization();
        groupBuilder.MapPost(GetAllCategoryImports, "get-all").RequireAuthorization();
        groupBuilder.MapGet(GetCategoryImportById, "{id}").RequireAuthorization();
        groupBuilder.MapPost(ConfirmCategoryImport, "{id}/confirm").RequireAuthorization();
    }

    [EndpointSummary("Create a new category import")]
    [EndpointDescription("Starts a category import for an uploaded file: creates a CategoryImport in the Processing state, capturing the blob path from the referenced file metadata.")]
    public static async Task<Results<Created<CategoryImportDto>, ProblemHttpResult>> CreateCategoryImport(
        ISender sender,
        CategoryImportRequests.CreateCategoryImportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCategoryImportCommand { FileMetadataId = request.FileMetadataId },
            cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Created($"/api/CategoryImports/{result.Value.Id}", result.Value);
    }

    [EndpointSummary("Get all category imports")]
    [EndpointDescription("Retrieves a paginated, filterable list of category imports.")]
    public static async Task<Results<Ok<PaginatedResponse<CategoryImportListItemDto>>, ProblemHttpResult>> GetAllCategoryImports(
        ISender sender,
        CategoryImportRequests.GetAllCategoryImportsRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetAllCategoryImportsQuery
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

    [EndpointSummary("Get a category import for review")]
    [EndpointDescription("Retrieves a category import's extracted category-name suggestions for review.")]
    public static async Task<Results<Ok<CategoryImportReviewDto>, ProblemHttpResult>> GetCategoryImportById(
        ISender sender,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCategoryImportByIdQuery { Id = id }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Confirm a category import")]
    [EndpointDescription("Confirms a reviewed category import using the explicit reviewed names.")]
    public static async Task<Results<Ok<CategoryImportConfirmationResultDto>, ProblemHttpResult>> ConfirmCategoryImport(
        ISender sender,
        Guid id,
        CategoryImportRequests.ConfirmCategoryImportRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmCategoryImportCommand
        {
            ImportId = id,
            CategoryNames = request.Names
        };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }
}
