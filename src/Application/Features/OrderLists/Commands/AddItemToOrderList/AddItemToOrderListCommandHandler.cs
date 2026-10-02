using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.OrderLists;

namespace skestock.Application.Features.OrderLists.Commands.AddItemToOrderList;

public class AddItemToOrderListCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<AddItemToOrderListCommand, Result<OrderListDto>>
{
    private const int MaxAttempts = 3;

    public async ValueTask<Result<OrderListDto>> Handle(AddItemToOrderListCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.Id == request.ItemId)
            .Select(i => new { i.Name, i.Unit })
            .SingleAsync(cancellationToken);

        // Creating a new list has nothing to conflict with; only the existing-list path retries.
        if (request.OrderListId is not { } orderListId)
        {
            var created = OrderList.Create(request.ClassId, request.NewOrderListName!.Trim(), null);
            created.Lines.Add(NewLine(item.Name, item.Unit, request));
            dbContext.OrderLists.Add(created);
            await dbContext.SaveChangesAsync(cancellationToken);

            return await LoadAsync(created.Id, cancellationToken);
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var orderList = await dbContext.OrderLists
                .Include(o => o.Lines)
                .SingleOrDefaultAsync(o => o.Id == orderListId, cancellationToken);

            if (orderList is null || orderList.ClassId != request.ClassId)
                return Result.Fail(new OrderListErrors.OrderListNotFound(orderListId));

            if (!orderList.IsEditable)
                return Result.Fail(new OrderListErrors.OrderListNotEditable(orderList.Id, orderList.Status.ToString()));

            var existing = orderList.Lines.FirstOrDefault(l => l.ItemId == request.ItemId);
            if (existing is not null)
                existing.Quantity += request.Quantity;
            else
                orderList.Lines.Add(NewLine(item.Name, item.Unit, request));

            orderList.TouchLines();

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return await LoadAsync(orderList.Id, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                // Another writer changed the list: drop stale tracked state and re-apply on fresh data.
                dbContext.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(new OrderListErrors.ConcurrencyConflict(orderListId));
            }
        }

        return Result.Fail(new OrderListErrors.ConcurrencyConflict(orderListId));
    }

    private static OrderListLine NewLine(string name, string unit, AddItemToOrderListCommand request) => new()
    {
        ItemId = request.ItemId,
        ProductName = name,
        Unit = unit,
        Quantity = request.Quantity
    };

    private async ValueTask<Result<OrderListDto>> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await OrderListProjection.LoadAsync(dbContext, id, cancellationToken);

        return dto is null
            ? Result.Fail(new OrderListErrors.OrderListNotFound(id))
            : Result.Ok(dto);
    }
}
