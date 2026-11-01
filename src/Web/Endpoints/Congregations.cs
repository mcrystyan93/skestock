using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.Congregations.Commands.CreateCongregation;
using skestock.Application.Features.Congregations.Commands.DeleteCongregation;
using skestock.Application.Features.Congregations.Commands.UpdateCongregation;
using skestock.Application.Features.Congregations.Models;
using skestock.Application.Features.Congregations.Queries.GetAllCongregations;
using skestock.Application.Features.Congregations.Queries.GetCongregationById;

namespace skestock.Web.Endpoints;

public class Congregations : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetAllCongregations, "get-all");
        groupBuilder.MapGet(GetCongregationById, "{id}");
        groupBuilder.MapPost(CreateCongregation, "");
        groupBuilder.MapPut(UpdateCongregation, "{id}");
        groupBuilder.MapDelete(DeleteCongregation, "{id}");
    }

    [EndpointSummary("Get all congregations")]
    [EndpointDescription("Retrieves congregations using keyset pagination, filtering and sorting.")]
    public static async Task<Results<Ok<PaginatedResponse<CongregationDto>>, ProblemHttpResult>> GetAllCongregations(
        ISender sender, CongregationRequests.GetAllCongregationsRequest request, CancellationToken cancellationToken)
    {
        var query = new GetAllCongregationsQuery
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

    [EndpointSummary("Get a congregation by id")]
    [EndpointDescription("Retrieves a single congregation by its id.")]
    public static async Task<Results<Ok<CongregationDto>, ProblemHttpResult>> GetCongregationById(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCongregationByIdQuery { Id = id }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Create a new congregation")]
    [EndpointDescription("Creates a new congregation.")]
    public static async Task<Results<Created<CongregationMutationDto>, ProblemHttpResult>> CreateCongregation(
        ISender sender, CongregationRequests.CreateCongregationRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCongregationCommand
        {
            Name = request.Name,
            Email = request.Email,
            Coordinator = request.Coordinator,
            CoordinatorPhone = request.CoordinatorPhone,
            DessertContactPerson = request.DessertContactPerson,
            DessertContactPhone = request.DessertContactPhone,
            CanReceiveStudents = request.CanReceiveStudents,
            IsSmallCongregation = request.IsSmallCongregation,
            Message = request.Message
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToCreated(v => $"/congregations/{v.Id}");
    }

    [EndpointSummary("Update an existing congregation")]
    [EndpointDescription("Updates an existing congregation.")]
    public static async Task<Results<Ok<CongregationMutationDto>, ProblemHttpResult>> UpdateCongregation(
        ISender sender, Guid id, CongregationRequests.UpdateCongregationRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCongregationCommand
        {
            Id = id,
            Name = request.Name,
            Email = request.Email,
            Coordinator = request.Coordinator,
            CoordinatorPhone = request.CoordinatorPhone,
            DessertContactPerson = request.DessertContactPerson,
            DessertContactPhone = request.DessertContactPhone,
            CanReceiveStudents = request.CanReceiveStudents,
            IsSmallCongregation = request.IsSmallCongregation,
            Message = request.Message
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Delete a congregation")]
    [EndpointDescription("Soft-deletes a congregation; it no longer appears in lists or lookups.")]
    public static async Task<Results<NoContent, ProblemHttpResult>> DeleteCongregation(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteCongregationCommand { Id = id }, cancellationToken);

        return result.ToNoContent();
    }
}
