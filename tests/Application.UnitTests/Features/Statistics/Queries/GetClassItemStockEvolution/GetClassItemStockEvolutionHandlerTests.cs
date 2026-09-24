using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Statistics.Queries.GetClassItemStockEvolution;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetClassItemStockEvolution;

public class GetClassItemStockEvolutionHandlerTests
{
    private static readonly DateOnly ClassStartDate = new(2026, 9, 1);
    private static readonly DateOnly ClassEndDate = new(2026, 9, 5);

    private static GoodsReceiptTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new GoodsReceiptTestDbContext(options);
    }

    private static async Task<(SchoolClass schoolClass, Item item, Location location, UserProfile user)> SeedBaseData(
        GoodsReceiptTestDbContext context)
    {
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = ClassStartDate,
            EndDate = ClassEndDate
        };
        var category = new Category { Name = "Stationery" };
        var item = new Item { Name = "Pencils", Sku = "PENCIL-001", Unit = "box", Category = category };
        var location = new Location { Name = "Classroom", Type = "Classroom" };
        var user = new UserProfile { IdentityId = Guid.NewGuid() };

        context.AddRange(schoolClass, category, item, location, user);
        await context.SaveChangesAsync(CancellationToken.None);

        return (schoolClass, item, location, user);
    }

    private static StockTransaction Transaction(
        SchoolClass schoolClass,
        Item item,
        Location location,
        UserProfile user,
        DateTimeOffset createdAt,
        int quantityChange) =>
        new()
        {
            Class = schoolClass,
            Item = item,
            Location = location,
            User = user,
            CreatedAt = createdAt,
            QuantityChange = quantityChange
        };

    private static GetClassItemStockEvolutionQuery Query(SchoolClass schoolClass, Item item) =>
        new() { ClassId = schoolClass.Id, ItemId = item.Id };

    [Test]
    public async Task Handle_StartsAtFirstTransactionAndGroupsSignedChangesByUtcDate()
    {
        await using var context = CreateContext();
        var (schoolClass, item, location, user) = await SeedBaseData(context);
        var previousDay = new DateTimeOffset(2026, 8, 31, 23, 59, 0, TimeSpan.Zero);
        var startDay = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
        var localStartDay = new DateTimeOffset(2026, 9, 2, 1, 0, 0, TimeSpan.FromHours(2));
        var middleDay = new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);
        var endDay = new DateTimeOffset(2026, 9, 5, 9, 0, 0, TimeSpan.Zero);
        var afterClassEnd = new DateTimeOffset(2026, 9, 7, 9, 0, 0, TimeSpan.Zero);

        context.StockTransactions.AddRange(
            Transaction(schoolClass, item, location, user, previousDay, 10),
            Transaction(schoolClass, item, location, user, startDay, 3),
            Transaction(schoolClass, item, location, user, localStartDay, 1),
            Transaction(schoolClass, item, location, user, middleDay, -2),
            Transaction(schoolClass, item, location, user, endDay, 1),
            Transaction(schoolClass, item, location, user, afterClassEnd, -4));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassItemStockEvolutionHandler(context)
            .Handle(Query(schoolClass, item), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Points
            .Select(point => (point.Date, point.CumulativeQuantity))
            .ShouldBe([
                (new DateOnly(2026, 8, 31), 10),
                (new DateOnly(2026, 9, 1), 14),
                (new DateOnly(2026, 9, 3), 12),
                (new DateOnly(2026, 9, 5), 13),
                (new DateOnly(2026, 9, 7), 9)
            ]);
        result.Value.ItemName.ShouldBe("Pencils");
        result.Value.Unit.ShouldBe("box");
    }

    [Test]
    public async Task Handle_NetsTransferTransactionsAcrossClassLocations()
    {
        await using var context = CreateContext();
        var (schoolClass, item, sourceLocation, user) = await SeedBaseData(context);
        var destinationLocation = new Location { Name = "Storage", Type = "StorageRoom" };
        context.Locations.Add(destinationLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        var transferDate = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        context.StockTransactions.AddRange(
            Transaction(schoolClass, item, sourceLocation, user, transferDate, -4),
            Transaction(schoolClass, item, destinationLocation, user, transferDate, 4));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassItemStockEvolutionHandler(context)
            .Handle(Query(schoolClass, item), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Points.Select(point => point.CumulativeQuantity).ShouldBe([0]);
    }

    [Test]
    public async Task Handle_IncludesTransactionsBeforeAndAfterClassPeriod()
    {
        await using var context = CreateContext();
        var (schoolClass, item, location, user) = await SeedBaseData(context);
        var beforeClass = new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero);
        var classStart = new DateTimeOffset(ClassStartDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var classEnd = new DateTimeOffset(ClassEndDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        var afterClass = new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);

        context.StockTransactions.AddRange(
            Transaction(schoolClass, item, location, user, beforeClass, 2),
            Transaction(schoolClass, item, location, user, classStart, 3),
            Transaction(schoolClass, item, location, user, classEnd, 4),
            Transaction(schoolClass, item, location, user, afterClass, 5));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassItemStockEvolutionHandler(context)
            .Handle(Query(schoolClass, item), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Points
            .Select(point => (point.Date, point.CumulativeQuantity))
            .ShouldBe([
                (new DateOnly(2026, 8, 30), 2),
                (ClassStartDate, 5),
                (ClassEndDate, 9),
                (new DateOnly(2026, 9, 6), 14)
            ]);
    }

    [Test]
    public async Task Handle_WithoutTransactionsReturnsNoPoints()
    {
        await using var context = CreateContext();
        var (schoolClass, item, _, _) = await SeedBaseData(context);

        var result = await new GetClassItemStockEvolutionHandler(context)
            .Handle(Query(schoolClass, item), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Points.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_WhenClassDoesNotExistReturnsNotFound()
    {
        await using var context = CreateContext();

        var result = await new GetClassItemStockEvolutionHandler(context)
            .Handle(new GetClassItemStockEvolutionQuery
            {
                ClassId = Guid.NewGuid(),
                ItemId = Guid.NewGuid()
            }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(error => error is SchoolClassErrors.SchoolClassNotFound);
    }

    [Test]
    public async Task Handle_WhenItemDoesNotExistReturnsNotFound()
    {
        await using var context = CreateContext();
        var (schoolClass, _, _, _) = await SeedBaseData(context);

        var result = await new GetClassItemStockEvolutionHandler(context)
            .Handle(new GetClassItemStockEvolutionQuery
            {
                ClassId = schoolClass.Id,
                ItemId = Guid.NewGuid()
            }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(error => error is ItemErrors.ItemNotFound);
    }
}
