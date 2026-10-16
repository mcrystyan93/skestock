using FluentResults;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SchoolClasses.Commands.CloseSchoolClass;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.SchoolClasses.Commands.CloseSchoolClass;

public class CloseSchoolClassCommandHandlerTests
{
    private GoodsReceiptTestDbContext _context = null!;
    private Guid _userId;
    private SchoolClass _source = null!;
    private SchoolClass _target = null!;
    private Item _item = null!;
    private Location _location = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = new GoodsReceiptTestDbContext(new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        _userId = Guid.NewGuid();
        _source = NewClass("Source", ClassStatus.Active);
        _target = NewClass("Target", ClassStatus.Upcoming);
        _item = new Item { Name = "Item", Category = new Category { Name = "Category" } };
        _location = new Location { Name = "Location", Type = "Storage" };
        _context.AddRange(_source, _target, _item, _location, new UserProfile { IdentityId = _userId });
        await _context.SaveChangesAsync(CancellationToken.None);
    }

    [TearDown]
    public async Task TearDown() => await _context.DisposeAsync();

    private static SchoolClass NewClass(string name, ClassStatus status) => new()
    {
        Name = name, StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31), Status = status
    };

    private StockBatch AddBatch(SchoolClass owner, int quantity, DateOnly? expiry = null, decimal price = 2.5m)
    {
        var batch = new StockBatch
        {
            Item = _item,
            Location = _location,
            ReceivedClass = owner,
            Quantity = quantity,
            ExpiryDate = expiry,
            ReceivedDate = new DateOnly(2026, 2, 3),
            UnitPrice = price
        };
        _context.StockBatches.Add(batch);
        return batch;
    }

    private async Task<Result> CloseAsync(Guid? id = null, Guid? targetId = null)
    {
        await _context.SaveChangesAsync(CancellationToken.None);
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(_userId);
        return await new CloseSchoolClassCommandHandler(_context, user.Object).Handle(
            new CloseSchoolClassCommand { Id = id ?? _source.Id, TargetClassId = targetId ?? _target.Id },
            CancellationToken.None);
    }

    private Task<List<StockBatch>> BatchesOf(SchoolClass owner) =>
        _context.StockBatches.AsNoTracking().Where(b => b.ReceivedClassId == owner.Id).ToListAsync();

    [Test]
    public async Task Handle_MovesPositiveBatchesToTargetCopyingEveryField()
    {
        var expiry = new DateOnly(2027, 1, 1);
        var original = AddBatch(_source, 7, expiry, 4.25m);

        var result = await CloseAsync();

        result.IsSuccess.ShouldBeTrue();
        var moved = (await BatchesOf(_target)).ShouldHaveSingleItem();
        moved.Quantity.ShouldBe(7);
        moved.ItemId.ShouldBe(_item.Id);
        moved.LocationId.ShouldBe(_location.Id);
        moved.ExpiryDate.ShouldBe(expiry);
        moved.ReceivedDate.ShouldBe(new DateOnly(2026, 2, 3));
        moved.UnitPrice.ShouldBe(4.25m);
        (await _context.StockBatches.AsNoTracking().SingleAsync(b => b.Id == original.Id)).Quantity.ShouldBe(0);
    }

    [Test]
    public async Task Handle_SkipsZeroQuantityBatches()
    {
        AddBatch(_source, 0);
        AddBatch(_source, 3);

        (await CloseAsync()).IsSuccess.ShouldBeTrue();

        (await BatchesOf(_target)).ShouldHaveSingleItem().Quantity.ShouldBe(3);
        (await _context.StockTransactions.CountAsync()).ShouldBe(2);
    }

    [Test]
    public async Task Handle_WritesPairedRolloverTransactions()
    {
        var original = AddBatch(_source, 5);

        (await CloseAsync()).IsSuccess.ShouldBeTrue();

        var transactions = await _context.StockTransactions.AsNoTracking().ToListAsync();
        transactions.Count.ShouldBe(2);
        transactions.ShouldAllBe(t => t.Type == StockTransactionType.Rollover && t.UserId == _userId);
        var outgoing = transactions.Single(t => t.QuantityChange == -5);
        outgoing.ClassId.ShouldBe(_source.Id);
        outgoing.BatchId.ShouldBe(original.Id);
        var incoming = transactions.Single(t => t.QuantityChange == 5);
        incoming.ClassId.ShouldBe(_target.Id);
        incoming.BatchId.ShouldBe((await BatchesOf(_target)).Single().Id);
    }

    [Test]
    public async Task Handle_ClosesTheSourceClassEvenWithoutStock()
    {
        (await CloseAsync()).IsSuccess.ShouldBeTrue();

        (await _context.SchoolClasses.AsNoTracking().SingleAsync(c => c.Id == _source.Id))
            .Status.ShouldBe(ClassStatus.Closed);
        (await _context.StockBatches.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task Handle_LeavesTheTargetsOwnStockUntouched()
    {
        var own = AddBatch(_target, 9);
        AddBatch(_source, 1);

        (await CloseAsync()).IsSuccess.ShouldBeTrue();

        (await _context.StockBatches.AsNoTracking().SingleAsync(b => b.Id == own.Id)).Quantity.ShouldBe(9);
        (await BatchesOf(_target)).Count.ShouldBe(2);
    }

    [Test]
    public async Task Handle_WhenSourceIsMissing_ReturnsNotFound()
    {
        var result = await CloseAsync(id: Guid.NewGuid());

        result.Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassNotFound);
    }

    [Test]
    public async Task Handle_WhenTargetIsMissing_ReturnsNotFoundAndKeepsSourceOpen()
    {
        AddBatch(_source, 4);

        var result = await CloseAsync(targetId: Guid.NewGuid());

        result.Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassNotFound);
        (await _context.SchoolClasses.AsNoTracking().SingleAsync(c => c.Id == _source.Id))
            .Status.ShouldBe(ClassStatus.Active);
    }

    [Test]
    public async Task Handle_WhenSourceAlreadyClosed_ReturnsAlreadyClosedWithoutMovingStock()
    {
        _source.Status = ClassStatus.Closed;
        AddBatch(_source, 4);

        var result = await CloseAsync();

        result.Errors.ShouldContain(e => e is SchoolClassErrors.AlreadyClosed);
        (await BatchesOf(_target)).ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_WhenTargetIsClosed_ReturnsTargetNotOpenWithoutMovingStock()
    {
        _target.Status = ClassStatus.Closed;
        AddBatch(_source, 4);

        var result = await CloseAsync();

        result.Errors.ShouldContain(e => e is SchoolClassErrors.TargetNotOpen);
        (await BatchesOf(_target)).ShouldBeEmpty();
        (await _context.SchoolClasses.AsNoTracking().SingleAsync(c => c.Id == _source.Id))
            .Status.ShouldBe(ClassStatus.Active);
    }

    [TestCase(ClassStatus.Upcoming)]
    [TestCase(ClassStatus.Active)]
    [TestCase(ClassStatus.Paused)]
    public async Task Handle_AcceptsAnyNonClosedTarget(ClassStatus targetStatus)
    {
        _target.Status = targetStatus;

        (await CloseAsync()).IsSuccess.ShouldBeTrue();
    }
}
