using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.OrderLists.Commands.DeleteOrderList;

public class DeleteOrderListCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteOrderListCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteOrderListCommand request, CancellationToken cancellationToken)
    {
        var orderList = await dbContext.OrderLists
            .SingleOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (orderList is null)
            return Result.Fail(new OrderListErrors.OrderListNotFound(request.Id));

        // Only Draft or Cancelled lists may be hard-deleted; a Submitted list must be cancelled first.
        if (orderList.Status is not (OrderListStatus.Draft or OrderListStatus.Cancelled))
            return Result.Fail(new OrderListErrors.OrderListNotDeletable(orderList.Id, orderList.Status.ToString()));

        dbContext.OrderLists.Remove(orderList);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
