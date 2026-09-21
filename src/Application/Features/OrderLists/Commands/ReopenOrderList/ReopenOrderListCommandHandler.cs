using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Features.OrderLists.Commands.ReopenOrderList;

public class ReopenOrderListCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ReopenOrderListCommand, Result<OrderListDto>>
{
    public async ValueTask<Result<OrderListDto>> Handle(
        ReopenOrderListCommand request,
        CancellationToken cancellationToken)
    {
        var orderList = await dbContext.OrderLists
            .SingleOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (orderList is null)
            return Result.Fail(new OrderListErrors.OrderListNotFound(request.Id));

        if (!orderList.IsReopenable)
            return Result.Fail(new OrderListErrors.OrderListNotReopenable(
                orderList.Id,
                orderList.Status.ToString()));

        orderList.Reopen();
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await OrderListProjection.LoadAsync(dbContext, orderList.Id, cancellationToken);
        if (dto is null)
            return Result.Fail(new OrderListErrors.OrderListNotFound(orderList.Id));

        return Result.Ok(dto);
    }
}
