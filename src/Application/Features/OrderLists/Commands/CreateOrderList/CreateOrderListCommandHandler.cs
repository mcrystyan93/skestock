using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.OrderLists.Commands.CreateOrderList;

public class CreateOrderListCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateOrderListCommand, Result<OrderListDto>>
{
    public async ValueTask<Result<OrderListDto>> Handle(CreateOrderListCommand request, CancellationToken cancellationToken)
    {
        var orderList = OrderList.Create(
            request.ClassId,
            string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim());

        var lines = await OrderListLineMapper.BuildLinesAsync(dbContext, request.Lines, cancellationToken);
        foreach (var line in lines)
            orderList.Lines.Add(line);

        dbContext.OrderLists.Add(orderList);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await OrderListProjection.LoadAsync(dbContext, orderList.Id, cancellationToken);
        if (dto is null)
            return Result.Fail(new OrderListErrors.OrderListNotFound(orderList.Id));

        return Result.Ok(dto);
    }
}
