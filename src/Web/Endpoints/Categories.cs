using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.Categories.Commands.CreateCategory;
using skestock.Application.Features.Categories.Models;
using skestock.Application.Features.Categories.Queries.GetAllCategories;
using skestock.Application.Features.Categories.Queries.GetCategoryById;

namespace skestock.Web.Endpoints;

public class Categories : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetAllCategories, "get-all");
        groupBuilder.MapGet(GetCategoryById, "{id}");
        groupBuilder.MapPost(CreateCategory, "");
    }

    [EndpointSummary("Get all categories")]
    [EndpointDescription("Retrieves all categories from the database.")]
    public static async Task<Results<Ok<PaginatedResponse<CategoryDto>>, ProblemHttpResult>> GetAllCategories(
        ISender sender, CategoryRequests.GetAllCategoriesRequest request, CancellationToken cancellationToken)
    {
        var query = new GetAllCategoriesQuery
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

    [EndpointSummary("Get a category by id")]
    [EndpointDescription("Retrieves a single category by its id.")]
    public static async Task<Results<Ok<CategoryDto>, ProblemHttpResult>> GetCategoryById(
        ISender sender, int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCategoryByIdQuery { Id = id }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Create a new category")]
    [EndpointDescription("Creates a new category in the database.")]
    public static async Task<Results<Created<CategoryDto>, ProblemHttpResult>> CreateCategory(
        ISender sender, CategoryRequests.CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand { Name = request.Name };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Created($"/categories/{result.Value.Id}", result.Value);
    }
}
