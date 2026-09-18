using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.OrderLists.Commands.CancelOrderList;

public class CancelOrderListCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CancelOrderListCommand, Result<OrderListDto>>
{
    public async ValueTask<Result<OrderListDto>> Handle(CancelOrderListCommand request, CancellationToken cancellationToken)
    {
        var orderList = await dbContext.OrderLists
            .SingleOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (orderList is null)
            return Result.Fail(new OrderListErrors.OrderListNotFound(request.Id));

        if (orderList.Status == OrderListStatus.Cancelled)
            return Result.Fail(new OrderListErrors.OrderListAlreadyCancelled(orderList.Id));

        orderList.Cancel();
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await OrderListProjection.LoadAsync(dbContext, orderList.Id, cancellationToken);
        if (dto is null)
            return Result.Fail(new OrderListErrors.OrderListNotFound(orderList.Id));

        return Result.Ok(dto);
    }
}
