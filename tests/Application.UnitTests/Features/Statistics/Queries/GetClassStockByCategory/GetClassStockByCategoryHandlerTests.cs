using Microsoft.EntityFrameworkCore;
using skestock.Application.Features.Statistics.Queries.GetClassStockByCategory;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetClassStockByCategory;

public class GetClassStockByCategoryHandlerTests
{
    private static GoodsReceiptTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new GoodsReceiptTestDbContext(options);
    }

    private static SchoolClass CreateClass() => new()
    {
        Name = "Fall 2026",
        StartDate = new DateOnly(2026, 9, 1),
        EndDate = new DateOnly(2026, 12, 20)
    };

    private static Category CreateCategory(string name) => new() { Name = name };

    private static Item CreateItem(Category category, string name, bool isActive = true) => new()
    {
        Name = name,
        Unit = "unit",
        Category = category,
        IsActive = isActive
    };

    private static Location CreateLocation(string name) => new()
    {
        Name = name,
        Type = "StorageRoom"
    };

    [Test]
    public async Task Handle_GroupsPositiveQuantitiesAcrossLocationsAndIncludesDisabledItems()
    {
        await using var context = CreateContext();
        var schoolClass = CreateClass();
        var pantry = CreateCategory("Pantry");
        var cleaning = CreateCategory("Cleaning");
        var activeItem = CreateItem(pantry, "Rice");
        var disabledItem = CreateItem(cleaning, "Soap", isActive: false);
        var firstLocation = CreateLocation("Kitchen");
        var secondLocation = CreateLocation("Storage");
        context.AddRange(schoolClass, pantry, cleaning, activeItem, disabledItem, firstLocation, secondLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.AddRange(
            new StockBatch
            {
                Item = activeItem,
                Location = firstLocation,
                ReceivedClass = schoolClass,
                Quantity = 4,
                ReceivedDate = new DateOnly(2026, 9, 1)
            },
            new StockBatch
            {
                Item = activeItem,
                Location = secondLocation,
                ReceivedClass = schoolClass,
                Quantity = 3,
                ReceivedDate = new DateOnly(2026, 9, 1)
            },
            new StockBatch
            {
                Item = disabledItem,
                Location = firstLocation,
                ReceivedClass = schoolClass,
                Quantity = 5,
                ReceivedDate = new DateOnly(2026, 9, 1)
            },
            new StockBatch
            {
                Item = activeItem,
                Location = firstLocation,
                ReceivedClass = schoolClass,
                Quantity = 0,
                ReceivedDate = new DateOnly(2026, 9, 1)
            },
            new StockBatch
            {
                Item = disabledItem,
                Location = secondLocation,
                ReceivedClass = schoolClass,
                Quantity = -2,
                ReceivedDate = new DateOnly(2026, 9, 1)
            });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassStockByCategoryHandler(context).Handle(
            new GetClassStockByCategoryQuery { ClassId = schoolClass.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalQuantity.ShouldBe(12);
        result.Value.TotalItemCount.ShouldBe(2);
        result.Value.TotalCategoryCount.ShouldBe(2);
        result.Value.Categories.ShouldContain(category =>
            category.CategoryId == pantry.Id &&
            category.CategoryName == "Pantry" &&
            category.Quantity == 7 &&
            category.ItemCount == 1);
        result.Value.Categories.ShouldContain(category =>
            category.CategoryId == cleaning.Id &&
            category.CategoryName == "Cleaning" &&
            category.Quantity == 5 &&
            category.ItemCount == 1);
    }

    [Test]
    public async Task Handle_WithLocationFilter_OnlyIncludesSelectedLocation()
    {
        await using var context = CreateContext();
        var schoolClass = CreateClass();
        var category = CreateCategory("Pantry");
        var item = CreateItem(category, "Rice");
        var selectedLocation = CreateLocation("Kitchen");
        var otherLocation = CreateLocation("Storage");
        context.AddRange(schoolClass, category, item, selectedLocation, otherLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.AddRange(
            new StockBatch
            {
                Item = item,
                Location = selectedLocation,
                ReceivedClass = schoolClass,
                Quantity = 4,
                ReceivedDate = new DateOnly(2026, 9, 1)
            },
            new StockBatch
            {
                Item = item,
                Location = otherLocation,
                ReceivedClass = schoolClass,
                Quantity = 9,
                ReceivedDate = new DateOnly(2026, 9, 1)
            });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassStockByCategoryHandler(context).Handle(
            new GetClassStockByCategoryQuery
            {
                ClassId = schoolClass.Id,
                LocationId = selectedLocation.Id
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalQuantity.ShouldBe(4);
        result.Value.Categories.Single().Quantity.ShouldBe(4);
    }

    [Test]
    public async Task Handle_WithUnknownClass_ReturnsSchoolClassNotFoundFailure()
    {
        await using var context = CreateContext();

        var result = await new GetClassStockByCategoryHandler(context).Handle(
            new GetClassStockByCategoryQuery { ClassId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Message.ShouldContain("School class");
    }
}
