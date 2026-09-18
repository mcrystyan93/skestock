using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Features.OrderLists.Commands.UpdateOrderList;

public class UpdateOrderListCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateOrderListCommand, Result<OrderListDto>>
{
    public async ValueTask<Result<OrderListDto>> Handle(UpdateOrderListCommand request, CancellationToken cancellationToken)
    {
        var orderList = await dbContext.OrderLists
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (orderList is null)
            return Result.Fail(new OrderListErrors.OrderListNotFound(request.Id));

        if (!orderList.IsEditable)
            return Result.Fail(new OrderListErrors.OrderListNotEditable(orderList.Id, orderList.Status.ToString()));

        orderList.Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();
        orderList.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        // Bulk-replace: drop the existing lines and rebuild from the request payload.
        dbContext.OrderListLines.RemoveRange(orderList.Lines);
        orderList.Lines.Clear();

        var lines = await OrderListLineMapper.BuildLinesAsync(dbContext, request.Lines, cancellationToken);
        foreach (var line in lines)
            orderList.Lines.Add(line);

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await OrderListProjection.LoadAsync(dbContext, orderList.Id, cancellationToken);
        if (dto is null)
            return Result.Fail(new OrderListErrors.OrderListNotFound(orderList.Id));

        return Result.Ok(dto);
    }
}
