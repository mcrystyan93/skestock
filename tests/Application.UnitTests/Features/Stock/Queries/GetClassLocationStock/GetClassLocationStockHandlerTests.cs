using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Filtering;
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
        var category = new Category
        {
            Name = "Groceries",
            Icon = new CategoryIcon("Groceries", "carrot", "/assets/icons/carrot.svg")
        };
        context.Categories.Add(category);

        var item = new Item
        {
            Name = "Rice",
            Sku = "RICE-001",
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

    private static ColumnFilter EqualsFilter(string field, Guid value) =>
        new(field, FilterOperator.Equals, value);

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
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("locationId", location.Id)]
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        var line = result.Value.Items.Single();
        line.ItemId.ShouldBe(item.Id);
        line.ItemName.ShouldBe("Rice");
        line.Sku.ShouldBe("RICE-001");
        line.CategoryIcon.ShouldNotBeNull();
        line.CategoryIcon!.FileName.ShouldBe("carrot");
        line.LocationId.ShouldBe(location.Id);
        line.LocationName.ShouldBe("Main Kitchen");
        line.Unit.ShouldBe("kg");
        line.Quantity.ShouldBe(18);
        line.IsLowStock.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_PerishableItemWithExpiredRemainingBatch_FlagsRowAndReport()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(
            context,
            minThreshold: 5,
            isPerishable: true);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        context.StockBatches.AddRange(
            new StockBatch
            {
                Item = item,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 4,
                ExpiryDate = today.AddDays(-1),
                ReceivedDate = today.AddDays(-10)
            },
            new StockBatch
            {
                Item = item,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 6,
                ExpiryDate = today.AddDays(5),
                ReceivedDate = today.AddDays(-2)
            });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("locationId", location.Id)]
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.HasExpiredItems.ShouldBeTrue();
        result.Value.Items.Single().IsExpired.ShouldBeTrue();
        result.Value.Items.Single().ExpiredQuantity.ShouldBe(4);
        result.Value.Items.Single().Quantity.ShouldBe(10);
    }

    [Test]
    public async Task Handle_PerishableItemWithFutureExpiry_IsNotFlagged()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(
            context,
            isPerishable: true);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        context.StockBatches.Add(
            new StockBatch
            {
                Item = item,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 4,
                ExpiryDate = today.AddDays(1),
                ReceivedDate = today
            });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("locationId", location.Id)]
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.HasExpiredItems.ShouldBeFalse();
        result.Value.Items.Single().IsExpired.ShouldBeFalse();
        result.Value.Items.Single().ExpiredQuantity.ShouldBe(0);
    }

    [Test]
    public async Task Handle_DepletedExpiredBatch_IsNotFlagged()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(
            context,
            isPerishable: true);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        context.StockBatches.Add(
            new StockBatch
            {
                Item = item,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 0,
                ExpiryDate = today.AddDays(-1),
                ReceivedDate = today.AddDays(-10)
            });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("locationId", location.Id)]
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.HasExpiredItems.ShouldBeFalse();
        result.Value.Items.Single().IsExpired.ShouldBeFalse();
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
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("locationId", location.Id)]
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Single().IsLowStock.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_LowStockOnly_ReturnsOnlyRowsBelowThreshold()
    {
        await using var context = CreateContext();
        var (lowStockItem, location, schoolClass) = await SeedBaseData(context, minThreshold: 10);
        var regularItem = new Item
        {
            Name = "Beans",
            Unit = "kg",
            MinThreshold = 10,
            CategoryId = lowStockItem.CategoryId
        };
        context.Items.Add(regularItem);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.AddRange(
            new StockBatch
            {
                Item = lowStockItem,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 5,
                ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            },
            new StockBatch
            {
                Item = regularItem,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 10,
                ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassLocationStockHandler(context).Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                LowStockOnly = true
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items.Single().ItemId.ShouldBe(lowStockItem.Id);
    }

    [Test]
    public async Task Handle_ExpiredOnly_ReturnsOnlyRowsWithExpiredRemainingStock()
    {
        await using var context = CreateContext();
        var (expiredItem, location, schoolClass) = await SeedBaseData(
            context,
            isPerishable: true);
        var freshItem = new Item
        {
            Name = "Canned beans",
            Unit = "unit",
            MinThreshold = 5,
            CategoryId = expiredItem.CategoryId
        };
        context.Items.Add(freshItem);
        await context.SaveChangesAsync(CancellationToken.None);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        context.StockBatches.AddRange(
            new StockBatch
            {
                Item = expiredItem,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 5,
                ExpiryDate = today.AddDays(-1),
                ReceivedDate = today.AddDays(-10)
            },
            new StockBatch
            {
                Item = freshItem,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 5,
                ExpiryDate = today.AddDays(10),
                ReceivedDate = today
            });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassLocationStockHandler(context).Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                ExpiredOnly = true
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items.Single().ItemId.ShouldBe(expiredItem.Id);
        result.Value.Items.Single().IsExpired.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_LowStockAndExpiredOnly_ReturnsOnlyRowsMatchingBothFilters()
    {
        await using var context = CreateContext();
        var (lowAndExpiredItem, location, schoolClass) = await SeedBaseData(
            context,
            minThreshold: 10,
            isPerishable: true);
        var lowOnlyItem = new Item
        {
            Name = "Fresh vegetables",
            Unit = "kg",
            MinThreshold = 10,
            IsPerishable = true,
            CategoryId = lowAndExpiredItem.CategoryId
        };
        var expiredOnlyItem = new Item
        {
            Name = "Expired canned beans",
            Unit = "unit",
            MinThreshold = 5,
            IsPerishable = true,
            CategoryId = lowAndExpiredItem.CategoryId
        };
        context.Items.AddRange(lowOnlyItem, expiredOnlyItem);
        await context.SaveChangesAsync(CancellationToken.None);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        context.StockBatches.AddRange(
            new StockBatch
            {
                Item = lowAndExpiredItem,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 5,
                ExpiryDate = today.AddDays(-1),
                ReceivedDate = today.AddDays(-10)
            },
            new StockBatch
            {
                Item = lowOnlyItem,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 5,
                ExpiryDate = today.AddDays(10),
                ReceivedDate = today
            },
            new StockBatch
            {
                Item = expiredOnlyItem,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 10,
                ExpiryDate = today.AddDays(-1),
                ReceivedDate = today.AddDays(-10)
            });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassLocationStockHandler(context).Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                LowStockOnly = true,
                ExpiredOnly = true
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items.Single().ItemId.ShouldBe(lowAndExpiredItem.Id);
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
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("locationId", location.Id)]
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Single().Quantity.ShouldBe(12);
    }

    [Test]
    public async Task Handle_NoBatchesForClassAndLocation_ReturnsEmptyList()
    {
        await using var context = CreateContext();
        var (_, location, schoolClass) = await SeedBaseData(context);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("locationId", location.Id)]
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
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
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("locationId", location.Id)],
                SearchTerm = "Ric"
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items.Single().ItemName.ShouldBe("Rice");
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
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("locationId", location.Id)],
                SearchTerm = "does-not-exist"
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
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
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("locationId", location.Id)],
                SearchTerm = "   "
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
    }

    [Test]
    public async Task Handle_CategoryFilter_ReturnsOnlyItemsInSelectedCategory()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context, minThreshold: 5);
        var otherCategory = new Category { Name = "Cleaning" };
        var otherItem = new Item
        {
            Name = "Soap",
            Unit = "unit",
            MinThreshold = 5,
            Category = otherCategory
        };
        context.Items.Add(otherItem);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.AddRange(
            new StockBatch
            {
                Item = item,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 10,
                ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            },
            new StockBatch
            {
                Item = otherItem,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 8,
                ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("categoryId", item.CategoryId)]
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items.Single().ItemId.ShouldBe(item.Id);
    }

    [Test]
    public async Task Handle_LocationAndCategoryFilters_ReturnOnlyMatchingStock()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context, minThreshold: 5);
        var otherLocation = new Location { Name = "Storage Room", Type = "StorageRoom" };
        var otherCategory = new Category { Name = "Cleaning" };
        var otherItem = new Item
        {
            Name = "Soap",
            Unit = "unit",
            MinThreshold = 5,
            Category = otherCategory
        };
        context.Locations.Add(otherLocation);
        context.Items.Add(otherItem);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.AddRange(
            new StockBatch
            {
                Item = item,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 10,
                ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            },
            new StockBatch
            {
                Item = item,
                Location = otherLocation,
                ReceivedClass = schoolClass,
                Quantity = 7,
                ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            },
            new StockBatch
            {
                Item = otherItem,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 6,
                ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters =
                [
                    EqualsFilter("locationId", location.Id),
                    EqualsFilter("categoryId", item.CategoryId)
                ]
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items.Single().ItemId.ShouldBe(item.Id);
        result.Value.Items.Single().LocationId.ShouldBe(location.Id);
        result.Value.Items.Single().Quantity.ShouldBe(10);
    }

    [Test]
    public async Task Handle_UnknownClassId_ReturnsSchoolClassNotFoundFailure()
    {
        await using var context = CreateContext();
        var (_, location, _) = await SeedBaseData(context);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery
            {
                ClassId = Guid.NewGuid(),
                Filters = [EqualsFilter("locationId", location.Id)]
            },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Message.ShouldContain("School class");
    }

    [Test]
    public async Task Handle_NoFilters_AggregatesAcrossEveryLocation_OneRowPerItemPerLocation()
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
            new GetClassLocationStockQuery { ClassId = schoolClass.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Items.ShouldContain(x => x.LocationId == location.Id && x.Quantity == 10);
        result.Value.Items.ShouldContain(x => x.LocationId == otherLocation.Id && x.Quantity == 7);
    }

    [Test]
    public async Task Handle_NoFilters_UnknownClassStillFails()
    {
        await using var context = CreateContext();

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Message.ShouldContain("School class");
    }

    [Test]
    public async Task Handle_UnmarkedZeroStock_RemainsVisibleInNormalMode()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context);

        context.StockBatches.Add(new StockBatch
        {
            Item = item,
            Location = location,
            ReceivedClass = schoolClass,
            Quantity = 0,
            ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassLocationStockHandler(context).Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items.Single().Quantity.ShouldBe(0);
        result.Value.Items.Single().HideWhenZeroStock.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_MarkedZeroStock_IsHiddenNormallyAndReturnedByIncludeHidden()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context);

        context.StockBatches.Add(new StockBatch
        {
            Item = item,
            Location = location,
            ReceivedClass = schoolClass,
            Quantity = 0,
            ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        context.ClassItemStockVisibilities.Add(new ClassItemStockVisibility
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id,
            HideWhenZeroStock = true
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var normalResult = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id },
            CancellationToken.None);
        var hiddenResult = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id, IncludeHidden = true },
            CancellationToken.None);

        normalResult.IsSuccess.ShouldBeTrue();
        normalResult.Value.Items.ShouldBeEmpty();
        hiddenResult.IsSuccess.ShouldBeTrue();
        hiddenResult.Value.Items.Count.ShouldBe(1);
        hiddenResult.Value.Items.Single().HideWhenZeroStock.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_MarkedStockVisibility_IsScopedToLocation()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context);
        var otherLocation = new Location { Name = "Storage Room", Type = "StorageRoom" };
        context.Locations.Add(otherLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.AddRange(
            new StockBatch
            {
                Item = item,
                Location = location,
                ReceivedClass = schoolClass,
                Quantity = 0,
                ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            },
            new StockBatch
            {
                Item = item,
                Location = otherLocation,
                ReceivedClass = schoolClass,
                Quantity = 5,
                ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            });
        context.ClassItemStockVisibilities.Add(new ClassItemStockVisibility
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id,
            HideWhenZeroStock = true
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var normalResult = await handler.Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("locationId", location.Id)]
            },
            CancellationToken.None);
        var hiddenResult = await handler.Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                IncludeHidden = true,
                Filters = [EqualsFilter("locationId", location.Id)]
            },
            CancellationToken.None);

        normalResult.IsSuccess.ShouldBeTrue();
        normalResult.Value.Items.ShouldBeEmpty();
        hiddenResult.IsSuccess.ShouldBeTrue();
        hiddenResult.Value.Items.Single().Quantity.ShouldBe(0);

        var otherLocationResult = await handler.Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id,
                Filters = [EqualsFilter("locationId", otherLocation.Id)]
            },
            CancellationToken.None);

        otherLocationResult.IsSuccess.ShouldBeTrue();
        otherLocationResult.Value.Items.Single().Quantity.ShouldBe(5);
        otherLocationResult.Value.Items.Single().HideWhenZeroStock.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_MarkedNegativeStock_IsReturnedOnlyByIncludeHidden()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context);

        context.StockBatches.Add(new StockBatch
        {
            Item = item,
            Location = location,
            ReceivedClass = schoolClass,
            Quantity = -2,
            ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        context.ClassItemStockVisibilities.Add(new ClassItemStockVisibility
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id,
            HideWhenZeroStock = true
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var normalResult = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id },
            CancellationToken.None);
        var hiddenResult = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id, IncludeHidden = true },
            CancellationToken.None);

        normalResult.Value.Items.ShouldBeEmpty();
        hiddenResult.Value.Items.Single().Quantity.ShouldBe(-2);
    }

    [Test]
    public async Task Handle_ReturnsLatestTransactionForSelectedClassItemAndLocationScope()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context);
        var otherLocation = new Location { Name = "Storage", Type = "Warehouse" };
        var otherClass = new SchoolClass
        {
            Name = "Spring 2027",
            StartDate = new DateOnly(2027, 2, 1),
            EndDate = new DateOnly(2027, 7, 31)
        };
        context.Locations.Add(otherLocation);
        context.SchoolClasses.Add(otherClass);
        context.StockBatches.Add(new StockBatch
        {
            Item = item,
            Location = location,
            ReceivedClass = schoolClass,
            Quantity = 5,
            ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        context.StockBatches.Add(new StockBatch
        {
            Item = item,
            Location = otherLocation,
            ReceivedClass = schoolClass,
            Quantity = 3,
            ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        var expectedLastUpdatedAt = new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);
        context.StockTransactions.AddRange(
            new StockTransaction
            {
                Item = item,
                Location = location,
                Class = schoolClass,
                CreatedAt = expectedLastUpdatedAt.AddDays(-1)
            },
            new StockTransaction
            {
                Item = item,
                Location = location,
                Class = schoolClass,
                CreatedAt = expectedLastUpdatedAt
            },
            new StockTransaction
            {
                Item = item,
                Location = location,
                Class = otherClass,
                CreatedAt = expectedLastUpdatedAt.AddDays(2)
            },
            new StockTransaction
            {
                Item = item,
                Location = otherLocation,
                Class = schoolClass,
                CreatedAt = expectedLastUpdatedAt.AddDays(3)
            });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery
            {
                ClassId = schoolClass.Id
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Items.Single(itemRow => itemRow.LocationId == location.Id)
            .LastUpdatedAt.ShouldBe(expectedLastUpdatedAt);
        result.Value.Items.Single(itemRow => itemRow.LocationId == otherLocation.Id)
            .LastUpdatedAt.ShouldBe(expectedLastUpdatedAt.AddDays(3));
    }

    [Test]
    public async Task Handle_WhenNoMatchingTransactionsExist_ReturnsNullLastUpdatedAt()
    {
        await using var context = CreateContext();
        var (item, location, schoolClass) = await SeedBaseData(context);
        context.StockBatches.Add(new StockBatch
        {
            Item = item,
            Location = location,
            ReceivedClass = schoolClass,
            Quantity = 5,
            ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClassLocationStockHandler(context);
        var result = await handler.Handle(
            new GetClassLocationStockQuery { ClassId = schoolClass.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Single().LastUpdatedAt.ShouldBeNull();
    }
}
