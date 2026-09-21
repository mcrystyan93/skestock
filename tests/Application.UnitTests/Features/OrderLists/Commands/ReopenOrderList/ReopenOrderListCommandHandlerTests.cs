using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.OrderLists.Commands.ReopenOrderList;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.OrderLists.Commands.ReopenOrderList;

public class ReopenOrderListCommandHandlerTests
{
    private static async Task<(OrderListTestDbContext Context, SchoolClass Class)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<OrderListTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new OrderListTestDbContext(options);

        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        context.SchoolClasses.Add(schoolClass);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, schoolClass);
    }

    [Test]
    public async Task ShouldReopenCancelledListAndClearSubmittedAt()
    {
        var (context, schoolClass) = await CreateContextAsync();
        await using var _ = context;

        var orderList = OrderList.Create(schoolClass.Id, "Order", null);
        orderList.Lines.Add(new OrderListLine { ProductName = "Napkins", Quantity = 2, Unit = "buc" });
        orderList.Submit();
        orderList.Cancel();
        context.OrderLists.Add(orderList);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ReopenOrderListCommandHandler(context);

        var result = await handler.Handle(
            new ReopenOrderListCommand { Id = orderList.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(OrderListStatus.Draft.ToString());
        result.Value.SubmittedAt.ShouldBeNull();
    }

    [Test]
    public async Task ShouldFailWhenListIsDraft()
    {
        var (context, schoolClass) = await CreateContextAsync();
        await using var _ = context;

        var orderList = OrderList.Create(schoolClass.Id, "Order", null);
        context.OrderLists.Add(orderList);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ReopenOrderListCommandHandler(context);

        var result = await handler.Handle(
            new ReopenOrderListCommand { Id = orderList.Id },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is OrderListErrors.OrderListNotReopenable);
    }

    [Test]
    public async Task ShouldFailWhenListIsSubmitted()
    {
        var (context, schoolClass) = await CreateContextAsync();
        await using var _ = context;

        var orderList = OrderList.Create(schoolClass.Id, "Order", null);
        orderList.Lines.Add(new OrderListLine { ProductName = "Napkins", Quantity = 2, Unit = "buc" });
        orderList.Submit();
        context.OrderLists.Add(orderList);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ReopenOrderListCommandHandler(context);

        var result = await handler.Handle(
            new ReopenOrderListCommand { Id = orderList.Id },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is OrderListErrors.OrderListNotReopenable);
    }

    [Test]
    public async Task ShouldFailWhenListIsNotFound()
    {
        var (context, _) = await CreateContextAsync();
        await using var _ = context;

        var handler = new ReopenOrderListCommandHandler(context);

        var result = await handler.Handle(
            new ReopenOrderListCommand { Id = Guid.NewGuid() },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is OrderListErrors.OrderListNotFound);
    }
}
