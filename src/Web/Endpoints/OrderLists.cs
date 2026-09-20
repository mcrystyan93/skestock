using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.OrderLists.Commands.CancelOrderList;
using skestock.Application.Features.OrderLists.Commands.CreateOrderList;
using skestock.Application.Features.OrderLists.Commands.DeleteOrderList;
using skestock.Application.Features.OrderLists.Commands.SubmitOrderList;
using skestock.Application.Features.OrderLists.Commands.UpdateOrderList;
using skestock.Application.Features.OrderLists.Models;
using skestock.Application.Features.OrderLists.Queries.GetAllOrderLists;
using skestock.Application.Features.OrderLists.Queries.GetOrderListById;

namespace skestock.Web.Endpoints;

public class OrderLists : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetAllOrderLists, "get-all");
        groupBuilder.MapGet(GetOrderListById, "{id}");
        groupBuilder.MapPost(CreateOrderList, "");
        groupBuilder.MapPut(UpdateOrderList, "{id}");
        groupBuilder.MapPost(SubmitOrderList, "{id}/submit");
        groupBuilder.MapPost(CancelOrderList, "{id}/cancel");
        groupBuilder.MapDelete(DeleteOrderList, "{id}");
    }

    [EndpointSummary("Get all order lists")]
    [EndpointDescription("Retrieves order lists using keyset pagination, filtering and sorting.")]
    public static async Task<Results<Ok<PaginatedResponse<OrderListListItemDto>>, ProblemHttpResult>> GetAllOrderLists(
        ISender sender, OrderListRequests.GetAllOrderListsRequest request, CancellationToken cancellationToken)
    {
        var query = new GetAllOrderListsQuery
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

    [EndpointSummary("Get an order list by id")]
    [EndpointDescription("Retrieves a single order list, including its lines.")]
    public static async Task<Results<Ok<OrderListDto>, ProblemHttpResult>> GetOrderListById(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrderListByIdQuery { Id = id }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Create a new order list")]
    [EndpointDescription("Creates a draft order list with optional initial lines.")]
    public static async Task<Results<Created<OrderListDto>, ProblemHttpResult>> CreateOrderList(
        ISender sender, OrderListRequests.CreateOrderListRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateOrderListCommand
        {
            ClassId = request.ClassId,
            Name = request.Name,
            Note = request.Note,
            Lines = request.Lines.Select(ToLineInput).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToCreated(v => $"/api/OrderLists/{v.Id}");
    }

    [EndpointSummary("Update an order list")]
    [EndpointDescription("Updates metadata and replaces the lines of a draft order list.")]
    public static async Task<Results<Ok<OrderListDto>, ProblemHttpResult>> UpdateOrderList(
        ISender sender, Guid id, OrderListRequests.UpdateOrderListRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateOrderListCommand
        {
            Id = id,
            Name = request.Name,
            Note = request.Note,
            Lines = request.Lines.Select(ToLineInput).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Submit an order list")]
    [EndpointDescription("Transitions a draft order list to submitted; it must contain at least one line.")]
    public static async Task<Results<Ok<OrderListDto>, ProblemHttpResult>> SubmitOrderList(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SubmitOrderListCommand { Id = id }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Cancel an order list")]
    [EndpointDescription("Marks an order list as cancelled.")]
    public static async Task<Results<Ok<OrderListDto>, ProblemHttpResult>> CancelOrderList(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelOrderListCommand { Id = id }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Delete an order list")]
    [EndpointDescription("Hard-deletes a draft or cancelled order list.")]
    public static async Task<Results<NoContent, ProblemHttpResult>> DeleteOrderList(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteOrderListCommand { Id = id }, cancellationToken);

        return result.ToNoContent();
    }

    private static OrderListLineInput ToLineInput(OrderListRequests.OrderListLineRequest line) => new()
    {
        ItemId = line.ItemId,
        ProductName = line.ProductName,
        Quantity = line.Quantity,
        Unit = line.Unit,
        Notes = line.Notes
    };
}
