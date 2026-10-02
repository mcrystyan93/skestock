using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.OrderLists.Commands.AddItemToOrderList;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.OrderLists.Commands.AddItemToOrderList;

public class AddItemToOrderListCommandHandlerTests
{
    [Test]
    public async Task Handle_AddsNewLineToExistingDraft()
    {
        var f = await CreateAsync();
        await using var _ = f.Context;
        var orderList = await SeedDraftAsync(f);

        var result = await new AddItemToOrderListCommandHandler(f.Context)
            .Handle(Command(f, orderListId: orderList.Id, quantity: 3), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var line = (await f.Context.OrderListLines.SingleAsync());
        line.ItemId.ShouldBe(f.Item.Id);
        line.ProductName.ShouldBe("Milk");
        line.Unit.ShouldBe("l");
        line.Quantity.ShouldBe(3);
    }

    [Test]
    public async Task Handle_WhenItemAlreadyOnList_AddsQuantityToExistingLine()
    {
        var f = await CreateAsync();
        await using var _ = f.Context;
        var orderList = await SeedDraftAsync(f);
        var handler = new AddItemToOrderListCommandHandler(f.Context);

        await handler.Handle(Command(f, orderListId: orderList.Id, quantity: 2), CancellationToken.None);
        var result = await handler.Handle(Command(f, orderListId: orderList.Id, quantity: 5), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await f.Context.OrderListLines.SingleAsync()).Quantity.ShouldBe(7);
    }

    [Test]
    public async Task Handle_WithNewName_CreatesDraftWithOneLine()
    {
        var f = await CreateAsync();
        await using var _ = f.Context;

        var result = await new AddItemToOrderListCommandHandler(f.Context)
            .Handle(Command(f, newName: "  Săptămâna 40  ", quantity: 1), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var list = await f.Context.OrderLists.Include(o => o.Lines).SingleAsync();
        list.Name.ShouldBe("Săptămâna 40");
        list.Status.ShouldBe(OrderListStatus.Draft);
        list.ClassId.ShouldBe(f.Class.Id);
        list.Lines.Count.ShouldBe(1);
    }

    [Test]
    public async Task Handle_WhenListNotDraft_ReturnsNotEditable()
    {
        var f = await CreateAsync();
        await using var _ = f.Context;
        var orderList = await SeedDraftAsync(f);
        orderList.Lines.Add(new OrderListLine { ProductName = "x", Quantity = 1, Unit = "buc" });
        orderList.Submit();
        await f.Context.SaveChangesAsync(CancellationToken.None);

        var result = await new AddItemToOrderListCommandHandler(f.Context)
            .Handle(Command(f, orderListId: orderList.Id, quantity: 1), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Metadata[ErrorMetadataKeys.Code].ShouldBe(OrderListErrors.OrderListNotEditable.ErrorCode);
    }

    [Test]
    public async Task Handle_WhenListBelongsToAnotherClass_ReturnsNotFound()
    {
        var f = await CreateAsync();
        await using var _ = f.Context;
        var orderList = await SeedDraftAsync(f);

        var command = new AddItemToOrderListCommand
        {
            ClassId = Guid.NewGuid(),
            ItemId = f.Item.Id,
            Quantity = 1,
            OrderListId = orderList.Id
        };
        var result = await new AddItemToOrderListCommandHandler(f.Context).Handle(command, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Metadata[ErrorMetadataKeys.Code].ShouldBe(OrderListErrors.OrderListNotFound.ErrorCode);
    }

    [Test]
    public async Task Handle_OnConcurrencyConflict_RetriesOnFreshDataAndSucceeds()
    {
        var f = await CreateAsync(throwOnce: true);
        await using var _ = f.Context;
        var orderList = await SeedDraftAsync(f);
        ((ConflictOnceContext)f.Context).ThrowOnNextSave = true;

        var result = await new AddItemToOrderListCommandHandler(f.Context)
            .Handle(Command(f, orderListId: orderList.Id, quantity: 4), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await f.Context.OrderListLines.SingleAsync()).Quantity.ShouldBe(4);
    }

    [Test]
    public async Task Handle_WhenConflictPersists_ReturnsConcurrencyConflict()
    {
        var f = await CreateAsync(throwOnce: true);
        await using var _ = f.Context;
        var orderList = await SeedDraftAsync(f);
        var context = (ConflictOnceContext)f.Context;
        context.AlwaysThrow = true;
        context.ThrowOnNextSave = true;

        var result = await new AddItemToOrderListCommandHandler(f.Context)
            .Handle(Command(f, orderListId: orderList.Id, quantity: 1), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Metadata[ErrorMetadataKeys.Code].ShouldBe(OrderListErrors.ConcurrencyConflict.ErrorCode);
    }

    private static AddItemToOrderListCommand Command(
        Fixture f, Guid? orderListId = null, string? newName = null, decimal quantity = 1) => new()
    {
        ClassId = f.Class.Id,
        ItemId = f.Item.Id,
        Quantity = quantity,
        OrderListId = orderListId,
        NewOrderListName = newName
    };

    private static async Task<OrderList> SeedDraftAsync(Fixture f)
    {
        var orderList = OrderList.Create(f.Class.Id, "Draft", null);
        f.Context.OrderLists.Add(orderList);
        await f.Context.SaveChangesAsync(CancellationToken.None);
        if (f.Context is ConflictOnceContext c) c.ThrowOnNextSave = false;
        return orderList;
    }

    private static async Task<Fixture> CreateAsync(bool throwOnce = false)
    {
        var options = new DbContextOptionsBuilder<OrderListTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        OrderListTestDbContext context = throwOnce ? new ConflictOnceContext(options) : new OrderListTestDbContext(options);

        var category = new Category { Name = "Dairy" };
        var item = new Item { Name = "Milk", Unit = "l", Category = category, IsActive = true };
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        context.Categories.Add(category);
        context.Items.Add(item);
        context.SchoolClasses.Add(schoolClass);
        await context.SaveChangesAsync(CancellationToken.None);

        return new Fixture(context, schoolClass, item);
    }

    private sealed record Fixture(OrderListTestDbContext Context, SchoolClass Class, Item Item);

    private sealed class ConflictOnceContext(DbContextOptions<OrderListTestDbContext> options)
        : OrderListTestDbContext(options)
    {
        public bool ThrowOnNextSave { get; set; }
        public bool AlwaysThrow { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowOnNextSave)
            {
                ThrowOnNextSave = AlwaysThrow;
                throw new DbUpdateConcurrencyException("Simulated conflict on OrderList.");
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
