using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Features.OrderLists.Commands.SubmitOrderList;

public class SubmitOrderListCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SubmitOrderListCommand, Result<OrderListDto>>
{
    public async ValueTask<Result<OrderListDto>> Handle(SubmitOrderListCommand request, CancellationToken cancellationToken)
    {
        var orderList = await dbContext.OrderLists
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (orderList is null)
            return Result.Fail(new OrderListErrors.OrderListNotFound(request.Id));

        if (!orderList.IsEditable)
            return Result.Fail(new OrderListErrors.OrderListNotEditable(orderList.Id, orderList.Status.ToString()));

        if (orderList.Lines.Count == 0)
            return Result.Fail(new OrderListErrors.OrderListEmpty(orderList.Id));

        orderList.Submit();
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await OrderListProjection.LoadAsync(dbContext, orderList.Id, cancellationToken);
        if (dto is null)
            return Result.Fail(new OrderListErrors.OrderListNotFound(orderList.Id));

        return Result.Ok(dto);
    }
}
