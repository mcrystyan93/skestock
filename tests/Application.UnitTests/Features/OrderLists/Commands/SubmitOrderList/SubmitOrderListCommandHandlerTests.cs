using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.OrderLists.Commands.SubmitOrderList;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.OrderLists.Commands.SubmitOrderList;

public class SubmitOrderListCommandHandlerTests
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
    public async Task ShouldSubmitDraftWithLines()
    {
        var (context, schoolClass) = await CreateContextAsync();
        await using var _ = context;

        var orderList = OrderList.Create(schoolClass.Id, "Order", null);
        orderList.Lines.Add(new OrderListLine { ProductName = "Napkins", Quantity = 2 });
        context.OrderLists.Add(orderList);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SubmitOrderListCommandHandler(context);

        var result = await handler.Handle(new SubmitOrderListCommand { Id = orderList.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(OrderListStatus.Submitted.ToString());
    }

    [Test]
    public async Task ShouldFailWhenListIsEmpty()
    {
        var (context, schoolClass) = await CreateContextAsync();
        await using var _ = context;

        var orderList = OrderList.Create(schoolClass.Id, "Order", null);
        context.OrderLists.Add(orderList);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SubmitOrderListCommandHandler(context);

        var result = await handler.Handle(new SubmitOrderListCommand { Id = orderList.Id }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is OrderListErrors.OrderListEmpty);
    }

    [Test]
    public async Task ShouldFailWhenNotEditable()
    {
        var (context, schoolClass) = await CreateContextAsync();
        await using var _ = context;

        var orderList = OrderList.Create(schoolClass.Id, "Order", null);
        orderList.Lines.Add(new OrderListLine { ProductName = "Napkins", Quantity = 2 });
        orderList.Submit();
        context.OrderLists.Add(orderList);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SubmitOrderListCommandHandler(context);

        var result = await handler.Handle(new SubmitOrderListCommand { Id = orderList.Id }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is OrderListErrors.OrderListNotEditable);
    }

    [Test]
    public async Task ShouldFailWhenNotFound()
    {
        var (context, _) = await CreateContextAsync();
        await using var _ = context;

        var handler = new SubmitOrderListCommandHandler(context);

        var result = await handler.Handle(new SubmitOrderListCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is OrderListErrors.OrderListNotFound);
    }
}
