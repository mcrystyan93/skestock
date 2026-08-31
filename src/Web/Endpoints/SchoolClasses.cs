using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.SchoolClasses.Commands.CreateSchoolClass;
using skestock.Application.Features.SchoolClasses.Commands.UpdateSchoolClass;
using skestock.Application.Features.SchoolClasses.Models;
using skestock.Application.Features.SchoolClasses.Queries.GetAllSchoolClasses;
using skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassById;
using skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassSummary;

namespace skestock.Web.Endpoints;

public class SchoolClasses : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetAllSchoolClasses, "get-all");
        groupBuilder.MapGet(GetSchoolClassById, "{id}");
        groupBuilder.MapGet(GetSchoolClassSummary, "{id}/summary");
        groupBuilder.MapPost(CreateSchoolClass, "");
        groupBuilder.MapPut(UpdateSchoolClass, "{id}");
    }

    [EndpointSummary("Get all school classes")]
    [EndpointDescription("Retrieves all school classes from the database.")]
    public static async Task<Results<Ok<PaginatedResponse<SchoolClassDto>>, ProblemHttpResult>> GetAllSchoolClasses(
        ISender sender, SchoolClassRequests.GetAllSchoolClassesRequest request, CancellationToken cancellationToken)
    {
        var query = new GetAllSchoolClassesQuery
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

    [EndpointSummary("Get a school class by id")]
    [EndpointDescription("Retrieves a single school class by its id.")]
    public static async Task<Results<Ok<SchoolClassDto>, ProblemHttpResult>> GetSchoolClassById(
        ISender sender, int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSchoolClassByIdQuery { Id = id }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Get a school class summary")]
    [EndpointDescription("Retrieves the goods-receipt summary (count and total amount) for a school class.")]
    public static async Task<Results<Ok<SchoolClassSummary>, ProblemHttpResult>> GetSchoolClassSummary(
        ISender sender, int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSchoolClassSummaryQuery { Id = id }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Create a new school class")]
    [EndpointDescription("Creates a new school class in the database.")]
    public static async Task<Results<Created<SchoolClassDto>, ProblemHttpResult>> CreateSchoolClass(
        ISender sender, SchoolClassRequests.CreateSchoolClassRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateSchoolClassCommand
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status
        };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Created($"/schoolclasses/{result.Value.Id}", result.Value);
    }

    [EndpointSummary("Update an existing school class")]
    [EndpointDescription("Updates an existing school class in the database.")]
    public static async Task<Results<Ok<SchoolClassDto>, ProblemHttpResult>> UpdateSchoolClass(
        ISender sender, int id, SchoolClassRequests.UpdateSchoolClassRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateSchoolClassCommand
        {
            Id = id,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status
        };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }
}
