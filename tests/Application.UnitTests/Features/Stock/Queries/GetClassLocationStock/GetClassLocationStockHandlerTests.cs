using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Features.Stock.Queries.GetClassLocationStock;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Stock.Queries.GetClassLocationStock;

/// <summary>
/// Reuses <see cref="GoodsReceiptTestDbContext"/> (already maps Item/Location/SchoolClass/
/// StockBatch with the relationships this handler's query needs) rather than defining another
/// near-identical in-memory DbContext.
/// </summary>
public class GetClassLocationStockHandlerTests
{
    private static GoodsReceiptTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new GoodsReceiptTestDbContext(options);
    }

    private static async Task<(Item item, Location location, SchoolClass schoolClass)> SeedBaseData(
        GoodsReceiptTestDbContext context, int minThreshold = 10, bool isPerishable = false)
    {
        var category = new Category { Name = "Groceries" };
        context.Categories.Add(category);

        var item = new Item
        {
            Name = "Rice",
            Unit = "kg",
            MinThreshold = minThreshold,
            IsPerishable = isPerishable,
            Category = category
        };
        context.Items.Add(item);

        var location = new Location { Name = "Main Kitchen", Type = "Kitchen" };
        context.Locations.Add(location);

        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 6, 30)
        };
        context.SchoolClasses.Add(schoolClass);

        await context.SaveChangesAsync(CancellationToken.None);

        return (item, location, schoolClass);
    }

    [Test]
    public async Task Handle_SumsQuantityAcrossMultipleBatches_ForMatchingClassAndLocation()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context, minThreshold: 5);

        context.StockBatches.AddRange(
            new StockBatch { Item = item, Location = location, ReceivedClass = schoolClass, Quantity = 10, ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow) },
            new StockBatch { Item = item, Location = location, ReceivedClass = schoolClass, Quantity = 8, ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow) });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id, LocationId = location.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(1);
        var line = result.Value.Single();
        line.ItemId.ShouldBe(item.Id);
        line.ItemName.ShouldBe("Rice");
        line.LocationId.ShouldBe(location.Id);
        line.LocationName.ShouldBe("Main Kitchen");
        line.Unit.ShouldBe("kg");
        line.Quantity.ShouldBe(18);
        line.IsLowStock.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_QuantityBelowMinThreshold_FlagsIsLowStock()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context, minThreshold: 20);

        context.StockBatches.Add(
            new StockBatch { Item = item, Location = location, ReceivedClass = schoolClass, Quantity = 5, ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow) });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id, LocationId = location.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Single().IsLowStock.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_BatchForDifferentLocation_IsExcludedFromSum()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context);
        var otherLocation = new Location { Name = "Storage Room", Type = "StorageRoom" };
        context.Locations.Add(otherLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.AddRange(
            new StockBatch { Item = item, Location = location, ReceivedClass = schoolClass, Quantity = 12, ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow) },
            new StockBatch { Item = item, Location = otherLocation, ReceivedClass = schoolClass, Quantity = 100, ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow) });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id, LocationId = location.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Single().Quantity.ShouldBe(12);
    }

    [Test]
    public async Task Handle_NoBatchesForClassAndLocation_ReturnsEmptyList()
    {
        await using var context = CreateContext();
        var (_, location, schoolClass) = await SeedBaseData(context);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id, LocationId = location.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_SearchTermMatchingItemName_ReturnsOnlyMatchingItems()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context, minThreshold: 5);

        var otherItem = new Item
        {
            Name = "Pasta",
            Unit = "kg",
            MinThreshold = 5,
            IsPerishable = false,
            Category = item.Category
        };
        context.Items.Add(otherItem);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.AddRange(
            new StockBatch { Item = item, Location = location, ReceivedClass = schoolClass, Quantity = 10, ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow) },
            new StockBatch { Item = otherItem, Location = location, ReceivedClass = schoolClass, Quantity = 6, ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow) });
        await context.SaveChangesAsync(CancellationToken.None);

        // The InMemory provider does string.Contains matching (case-sensitive, unlike SQL
        // Server's default case-insensitive collation used in production/functional tests), so
        // the term's casing must match the seeded item name here.
        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id, LocationId = location.Id, SearchTerm = "Ric" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(1);
        result.Value.Single().ItemName.ShouldBe("Rice");
    }

    [Test]
    public async Task Handle_SearchTermWithNoMatches_ReturnsEmptyList()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context, minThreshold: 5);

        context.StockBatches.Add(
            new StockBatch { Item = item, Location = location, ReceivedClass = schoolClass, Quantity = 10, ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow) });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id, LocationId = location.Id, SearchTerm = "does-not-exist" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_BlankSearchTerm_IsTreatedAsNoFilter()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context, minThreshold: 5);

        context.StockBatches.Add(
            new StockBatch { Item = item, Location = location, ReceivedClass = schoolClass, Quantity = 10, ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow) });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id, LocationId = location.Id, SearchTerm = "   " },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(1);
    }

    [Test]
    public async Task Handle_UnknownClassId_ReturnsSchoolClassNotFoundFailure()
    {
        await using var context = CreateContext();
        var (_, location, _) = await SeedBaseData(context);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = Guid.NewGuid(), LocationId = location.Id },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Message.ShouldContain("School class");
    }

    [Test]
    public async Task Handle_UnknownLocationId_ReturnsLocationNotFoundFailure()
    {
        await using var context = CreateContext();
        var (_, _, schoolClass) = await SeedBaseData(context);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id, LocationId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Message.ShouldContain("Location");
    }

    [Test]
    public async Task Handle_NullLocationId_AggregatesAcrossEveryLocation_OneRowPerItemPerLocation()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context, minThreshold: 5);
        var otherLocation = new Location { Name = "Storage Room", Type = "StorageRoom" };
        context.Locations.Add(otherLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.AddRange(
            new StockBatch { Item = item, Location = location, ReceivedClass = schoolClass, Quantity = 10, ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow) },
            new StockBatch { Item = item, Location = otherLocation, ReceivedClass = schoolClass, Quantity = 7, ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow) });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id, LocationId = null },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldContain(x => x.LocationId == location.Id && x.Quantity == 10);
        result.Value.ShouldContain(x => x.LocationId == otherLocation.Id && x.Quantity == 7);
    }

    [Test]
    public async Task Handle_NullLocationId_DoesNotCheckLocationExistence_UnknownClassStillFails()
    {
        await using var context = CreateContext();

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = Guid.NewGuid(), LocationId = null },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Message.ShouldContain("School class");
    }
}
