using FluentResults;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Errors;
using skestock.Application.Features.GoodsReceipts.Commands.ConfirmGoodsReceiptImport;
using skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;
using skestock.Application.Features.OrderLists.Commands.AddItemToOrderList;
using skestock.Application.Features.OrderLists.Commands.CancelOrderList;
using skestock.Application.Features.OrderLists.Commands.CreateOrderList;
using skestock.Application.Features.OrderLists.Commands.DeleteOrderList;
using skestock.Application.Features.OrderLists.Commands.ReopenOrderList;
using skestock.Application.Features.OrderLists.Commands.SubmitOrderList;
using skestock.Application.Features.OrderLists.Commands.UpdateOrderList;
using skestock.Application.Features.SchoolClasses.Commands.InitializeSchoolClassConfiguration;
using skestock.Application.Features.SchoolClasses.Commands.UpdateClassDepartmentResponsiblePerson;
using skestock.Application.Features.Stock.Commands.AdjustStock;
using skestock.Application.Features.Stock.Commands.ExtendExpiredStockExpiry;
using skestock.Application.Features.Stock.Commands.MoveStock;
using skestock.Application.Features.Stock.Commands.RemoveExpiredStock;
using skestock.Application.Features.Stock.Commands.SetClassItemStockVisibility;
using skestock.Application.Features.StockBatches.Commands.CreateStockBatch;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Application.UnitTests.Features.OrderLists;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Common.Behaviours;

public record ClassScopedTestCommand(Guid? SchoolClassId) : IRequest<Result>, IRequiresOpenClass
{
    Guid? IRequiresOpenClass.ClassId => SchoolClassId;
}

public class ClassOpenBehaviourTests
{
    private GoodsReceiptTestDbContext _context = null!;
    private bool _handlerRan;

    [SetUp]
    public void SetUp()
    {
        _context = new GoodsReceiptTestDbContext(new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        _orderListContext = new OrderListTestDbContext(new DbContextOptionsBuilder<OrderListTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        _handlerRan = false;
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
        await _orderListContext.DisposeAsync();
    }

    private async Task<SchoolClass> AddClassAsync(ClassStatus status)
    {
        var schoolClass = new SchoolClass
        {
            Name = "Class", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31), Status = status
        };
        _context.SchoolClasses.Add(schoolClass);
        await _context.SaveChangesAsync(CancellationToken.None);
        return schoolClass;
    }

    private ValueTask<Result> HandleAsync(Guid? classId) =>
        new ClassOpenBehaviour<ClassScopedTestCommand, Result>(_context).Handle(
            new ClassScopedTestCommand(classId),
            (_, _) =>
            {
                _handlerRan = true;
                return ValueTask.FromResult(Result.Ok());
            },
            CancellationToken.None);

    [Test]
    public async Task Handle_WhenTheClassIsClosed_FailsWithoutRunningTheHandler()
    {
        var schoolClass = await AddClassAsync(ClassStatus.Closed);

        var result = await HandleAsync(schoolClass.Id);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassClosed);
        _handlerRan.ShouldBeFalse();
    }

    [TestCase(ClassStatus.Upcoming)]
    [TestCase(ClassStatus.Active)]
    [TestCase(ClassStatus.Paused)]
    public async Task Handle_WhenTheClassIsOpen_RunsTheHandler(ClassStatus status)
    {
        var schoolClass = await AddClassAsync(status);

        var result = await HandleAsync(schoolClass.Id);

        result.IsSuccess.ShouldBeTrue();
        _handlerRan.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WhenTheClassDoesNotExist_LeavesTheNotFoundToTheHandler()
    {
        (await HandleAsync(Guid.NewGuid())).IsSuccess.ShouldBeTrue();
        _handlerRan.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WhenNoClassIsNamed_RunsTheHandler()
    {
        (await HandleAsync(null)).IsSuccess.ShouldBeTrue();
        _handlerRan.ShouldBeTrue();
    }

    private sealed record OrderListTestCommand(Guid ListId) : IRequest<Result>, IRequiresOpenClass
    {
        Guid? IRequiresOpenClass.OrderListId => ListId;
    }

    private OrderListTestDbContext _orderListContext = null!;

    private ValueTask<Result> HandleOrderListAsync(OrderListTestCommand command) =>
        new ClassOpenBehaviour<OrderListTestCommand, Result>(_orderListContext).Handle(
            command, (_, _) => ValueTask.FromResult(Result.Ok()), CancellationToken.None);

    private async Task<SchoolClass> AddOrderListClassAsync(ClassStatus status)
    {
        var schoolClass = new SchoolClass
        {
            Name = "Class", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31), Status = status
        };
        _orderListContext.SchoolClasses.Add(schoolClass);
        await _orderListContext.SaveChangesAsync(CancellationToken.None);
        return schoolClass;
    }

    [Test]
    public async Task Handle_ResolvesTheClassOfAnOrderListOncePerRequest()
    {
        var open = await AddOrderListClassAsync(ClassStatus.Active);
        var closed = await AddOrderListClassAsync(ClassStatus.Closed);
        var list = OrderList.Create(open.Id, null, null);
        _orderListContext.OrderLists.Add(list);
        await _orderListContext.SaveChangesAsync(CancellationToken.None);
        var command = new OrderListTestCommand(list.Id);

        (await HandleOrderListAsync(command)).IsSuccess.ShouldBeTrue();

        // A second lookup would now find the closed class; the remembered answer proves there is none.
        list.ClassId = closed.Id;
        await _orderListContext.SaveChangesAsync(CancellationToken.None);
        (await HandleOrderListAsync(command)).IsSuccess.ShouldBeTrue();
        (await HandleOrderListAsync(new OrderListTestCommand(list.Id))).IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_DoesNotRememberAnUnknownOrderList()
    {
        var closed = await AddOrderListClassAsync(ClassStatus.Closed);
        var list = OrderList.Create(closed.Id, null, null);
        var command = new OrderListTestCommand(list.Id);

        (await HandleOrderListAsync(command)).IsSuccess.ShouldBeTrue();

        _orderListContext.OrderLists.Add(list);
        await _orderListContext.SaveChangesAsync(CancellationToken.None);
        (await HandleOrderListAsync(command)).IsFailed.ShouldBeTrue();
    }

    private sealed record AmbiguousTestCommand : IRequest<Result>, IRequiresOpenClass
    {
        Guid? IRequiresOpenClass.ClassId => Guid.NewGuid();
        Guid? IRequiresOpenClass.OrderListId => Guid.NewGuid();
    }

    [Test]
    public void Handle_WhenACommandNamesMoreThanOneTarget_Throws() =>
        Should.Throw<InvalidOperationException>(async () =>
            await new ClassOpenBehaviour<AmbiguousTestCommand, Result>(_context).Handle(
                new AmbiguousTestCommand(), (_, _) => ValueTask.FromResult(Result.Ok()), CancellationToken.None));

    // Guards against adding a class-scoped command and forgetting to protect it.
    [TestCase(typeof(AdjustStockCommand))]
    [TestCase(typeof(MoveStockCommand))]
    [TestCase(typeof(RemoveExpiredStockCommand))]
    [TestCase(typeof(ExtendExpiredStockExpiryCommand))]
    [TestCase(typeof(SetClassItemStockVisibilityCommand))]
    [TestCase(typeof(CreateStockBatchCommand))]
    [TestCase(typeof(CreateGoodsReceiptCommand))]
    [TestCase(typeof(CreateGoodsReceiptImportCommand))]
    [TestCase(typeof(ConfirmGoodsReceiptImportCommand))]
    [TestCase(typeof(CreateOrderListCommand))]
    [TestCase(typeof(AddItemToOrderListCommand))]
    [TestCase(typeof(UpdateOrderListCommand))]
    [TestCase(typeof(SubmitOrderListCommand))]
    [TestCase(typeof(CancelOrderListCommand))]
    [TestCase(typeof(ReopenOrderListCommand))]
    [TestCase(typeof(DeleteOrderListCommand))]
    [TestCase(typeof(InitializeSchoolClassConfigurationCommand))]
    [TestCase(typeof(UpdateClassDepartmentResponsiblePersonCommand))]
    public void ClassScopedCommands_RequireAnOpenClass(Type command) =>
        typeof(IRequiresOpenClass).IsAssignableFrom(command).ShouldBeTrue();
}
