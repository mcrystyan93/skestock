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
        _handlerRan = false;
    }

    [TearDown]
    public async Task TearDown() => await _context.DisposeAsync();

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
