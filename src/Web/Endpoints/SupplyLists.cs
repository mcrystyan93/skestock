using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.SupplyLists.Commands.CreateSupplyList;
using skestock.Application.Features.SupplyLists.Commands.DisableSupplyList;
using skestock.Application.Features.SupplyLists.Commands.EnableSupplyList;
using skestock.Application.Features.SupplyLists.Commands.UpdateSupplyList;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Application.Features.SupplyLists.Queries.GetAllSupplyLists;
using skestock.Application.Features.SupplyLists.Queries.GetSupplyListById;

namespace skestock.Web.Endpoints;

public class SupplyLists : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetAllSupplyLists, "get-all");
        groupBuilder.MapGet(GetSupplyListById, "{id}");
        groupBuilder.MapPost(CreateSupplyList, "");
        groupBuilder.MapPut(UpdateSupplyList, "{id}");
        groupBuilder.MapPost(DisableSupplyList, "{id}/disable");
        groupBuilder.MapPost(EnableSupplyList, "{id}/enable");
    }

    [EndpointSummary("Get all supply lists")]
    [EndpointDescription("Retrieves supply lists using keyset pagination, filtering and sorting.")]
    public static async Task<Results<Ok<PaginatedResponse<SupplyListListItemDto>>, ProblemHttpResult>> GetAllSupplyLists(
        ISender sender, SupplyListRequests.GetAllSupplyListsRequest request, CancellationToken cancellationToken)
    {
        var query = new GetAllSupplyListsQuery
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

    [EndpointSummary("Get a supply list by id")]
    [EndpointDescription("Retrieves a single supply list, including its lines.")]
    public static async Task<Results<Ok<SupplyListDto>, ProblemHttpResult>> GetSupplyListById(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSupplyListByIdQuery { Id = id }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Create a new supply list")]
    [EndpointDescription("Creates an active supply list with a frequency and optional initial lines.")]
    public static async Task<Results<Created<SupplyListDto>, ProblemHttpResult>> CreateSupplyList(
        ISender sender, SupplyListRequests.CreateSupplyListRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateSupplyListCommand
        {
            Name = request.Name,
            Note = request.Note,
            Frequency = request.Frequency,
            IntervalWeeks = request.IntervalWeeks,
            Lines = request.Lines.Select(ToLineInput).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToCreated(v => $"/api/SupplyLists/{v.Id}");
    }

    [EndpointSummary("Update a supply list")]
    [EndpointDescription("Updates metadata and replaces the lines of an active supply list.")]
    public static async Task<Results<Ok<SupplyListDto>, ProblemHttpResult>> UpdateSupplyList(
        ISender sender, Guid id, SupplyListRequests.UpdateSupplyListRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateSupplyListCommand
        {
            Id = id,
            Name = request.Name,
            Note = request.Note,
            Frequency = request.Frequency,
            IntervalWeeks = request.IntervalWeeks,
            Lines = request.Lines.Select(ToLineInput).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Disable a supply list")]
    [EndpointDescription("Deactivates a supply list; a disabled list cannot be modified.")]
    public static async Task<Results<Ok<SupplyListDto>, ProblemHttpResult>> DisableSupplyList(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DisableSupplyListCommand { Id = id }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Enable a supply list")]
    [EndpointDescription("Reactivates a disabled supply list.")]
    public static async Task<Results<Ok<SupplyListDto>, ProblemHttpResult>> EnableSupplyList(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new EnableSupplyListCommand { Id = id }, cancellationToken);

        return result.ToOk();
    }

    private static SupplyListLineInput ToLineInput(SupplyListRequests.SupplyListLineRequest line) => new()
    {
        ItemId = line.ItemId,
        Quantity = line.Quantity,
        Unit = line.Unit,
        Notes = line.Notes
    };
}
