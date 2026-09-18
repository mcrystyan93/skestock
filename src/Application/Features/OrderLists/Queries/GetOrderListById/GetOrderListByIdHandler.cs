using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Features.OrderLists.Queries.GetOrderListById;

public class GetOrderListByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetOrderListByIdQuery, Result<OrderListDto>>
{
    public async ValueTask<Result<OrderListDto>> Handle(GetOrderListByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await OrderListProjection.LoadAsync(dbContext, request.Id, cancellationToken);
        if (dto is null)
            return Result.Fail(new OrderListErrors.OrderListNotFound(request.Id));

        return Result.Ok(dto);
    }
}
