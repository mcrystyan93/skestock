using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.Locations.Commands.CreateLocation;
using skestock.Application.Features.Locations.Commands.UpdateLocation;
using skestock.Application.Features.Locations.Models;
using skestock.Application.Features.Locations.Queries.GetAllLocations;
using skestock.Application.Features.Locations.Queries.GetLocationById;

namespace skestock.Web.Endpoints;

public class Locations : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetAllLocations, "get-all");
        groupBuilder.MapGet(GetLocationById, "{id}");
        groupBuilder.MapPost(CreateLocation, "");
        groupBuilder.MapPut(UpdateLocation, "{id}");
    }

    [EndpointSummary("Get all locations")]
    [EndpointDescription("Retrieves all locations from the database.")]
    public static async Task<Results<Ok<PaginatedResponse<LocationDto>>, ProblemHttpResult>> GetAllLocations(
        ISender sender, LocationRequests.GetAllLocationsRequest request, CancellationToken cancellationToken)
    {
        var query = new GetAllLocationsQuery
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

    [EndpointSummary("Get a location by id")]
    [EndpointDescription("Retrieves a single location by its id.")]
    public static async Task<Results<Ok<LocationDto>, ProblemHttpResult>> GetLocationById(
        ISender sender, int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLocationByIdQuery { Id = id }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Create a new location")]
    [EndpointDescription("Creates a new location in the database.")]
    public static async Task<Results<Created<LocationDto>, ProblemHttpResult>> CreateLocation(
        ISender sender, LocationRequests.CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateLocationCommand
        {
            Name = request.Name,
            Type = request.Type,
            ParentLocationId = request.ParentLocationId
        };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Created($"/locations/{result.Value.Id}", result.Value);
    }

    [EndpointSummary("Update an existing location")]
    [EndpointDescription("Updates an existing location in the database.")]
    public static async Task<Results<Ok<LocationDto>, ProblemHttpResult>> UpdateLocation(
        ISender sender, int id, LocationRequests.UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateLocationCommand
        {
            Id = id,
            Name = request.Name,
            Type = request.Type,
            ParentLocationId = request.ParentLocationId
        };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }
}
