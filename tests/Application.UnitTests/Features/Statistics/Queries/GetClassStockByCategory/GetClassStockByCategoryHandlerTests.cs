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

    private static SchoolClass CreateClass(string name = "Fall 2026") => new()
    {
        Name = name, StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 20)
    };

    private static Category CreateCategory(string name) => new() { Name = name };

    private static Item CreateItem(
        Category category,
        string name,
        string unit = "unit",
        bool isActive = true) => new() { Name = name, Unit = unit, Category = category, IsActive = isActive };

    private static Location CreateLocation(string name) => new() { Name = name, Type = "StorageRoom" };

    private static StockBatch CreateBatch(
        Item item,
        Location location,
        SchoolClass schoolClass,
        int quantity) => new()
    {
        Item = item,
        Location = location,
        ReceivedClass = schoolClass,
        Quantity = quantity,
        ReceivedDate = new DateOnly(2026, 9, 1)
    };

    [Test]
    public async Task Handle_AllLocations_ReturnsSignedCategorySeriesAndOmitsUnstockedLocations()
    {
        await using var context = CreateContext();
        var schoolClass = CreateClass();
        var otherClass = CreateClass("Winter 2027");
        var pantry = CreateCategory("Pantry");
        var cleaning = CreateCategory("Cleaning");
        var dormant = CreateCategory("Dormant");
        var otherClassCategory = CreateCategory("Other class only");
        var rice = CreateItem(pantry, "Rice", unit: "kg");
        var beans = CreateItem(pantry, "Beans", unit: "box");
        var disabledItem = CreateItem(cleaning, "Soap", unit: "box", isActive: false);
        var zeroItem = CreateItem(dormant, "Unused");
        var otherClassItem = CreateItem(otherClassCategory, "Foreign stock");
        var kitchen = CreateLocation("Kitchen");
        var storage = CreateLocation("Storage");
        var unstockedLocation = CreateLocation("Unused");
        context.AddRange(
            schoolClass,
            otherClass,
            pantry,
            cleaning,
            dormant,
            otherClassCategory,
            rice,
            beans,
            disabledItem,
            zeroItem,
            otherClassItem,
            kitchen,
            storage,
            unstockedLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.AddRange(
            CreateBatch(rice, kitchen, schoolClass, 4),
            CreateBatch(rice, storage, schoolClass, 3),
            CreateBatch(beans, kitchen, schoolClass, 2),
            CreateBatch(disabledItem, kitchen, schoolClass, 5),
            CreateBatch(disabledItem, storage, schoolClass, -2),
            CreateBatch(zeroItem, kitchen, schoolClass, 0),
            CreateBatch(otherClassItem, unstockedLocation, otherClass, 99));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassStockByCategoryHandler(context).Handle(
            new GetClassStockByCategoryQuery { ClassId = schoolClass.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Labels.ShouldBe(["Kitchen", "Storage"]);
        result.Value.Series.Select(series => series.Name)
            .ShouldBe(["Cleaning", "Dormant", "Pantry"]);
        result.Value.Series.Single(series => series.Name == "Cleaning")
            .Data.ShouldBe([5, -2]);
        result.Value.Series.Single(series => series.Name == "Dormant")
            .Data.ShouldBe([0, 0]);
        result.Value.Series.Single(series => series.Name == "Pantry")
            .Data.ShouldBe([6, 3]);
    }

    [Test]
    public async Task Handle_WithLocation_ReturnsFullClassCategorySetAndZeroFillsMissingStock()
    {
        await using var context = CreateContext();
        var schoolClass = CreateClass();
        var pantry = CreateCategory("Pantry");
        var cleaning = CreateCategory("Cleaning");
        var rice = CreateItem(pantry, "Rice");
        var soap = CreateItem(cleaning, "Soap");
        var selectedLocation = CreateLocation("Kitchen");
        var otherLocation = CreateLocation("Storage");
        context.AddRange(schoolClass, pantry, cleaning, rice, soap, selectedLocation, otherLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.AddRange(
            CreateBatch(rice, selectedLocation, schoolClass, 4),
            CreateBatch(soap, otherLocation, schoolClass, 9));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassStockByCategoryHandler(context).Handle(
            new GetClassStockByCategoryQuery { ClassId = schoolClass.Id, LocationId = selectedLocation.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Labels.ShouldBe(["Kitchen"]);
        result.Value.Series.Select(series => series.Name)
            .ShouldBe(["Cleaning", "Pantry"]);
        result.Value.Series.Single(series => series.Name == "Cleaning")
            .Data.ShouldBe([0]);
        result.Value.Series.Single(series => series.Name == "Pantry")
            .Data.ShouldBe([4]);
    }

    [Test]
    public async Task Handle_WithExistingLocationWithoutClassBatches_ReturnsZeroForClassCategories()
    {
        await using var context = CreateContext();
        var schoolClass = CreateClass();
        var category = CreateCategory("Pantry");
        var item = CreateItem(category, "Rice");
        var stockedLocation = CreateLocation("Kitchen");
        var emptyLocation = CreateLocation("Storage");
        context.AddRange(schoolClass, category, item, stockedLocation, emptyLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.Add(CreateBatch(item, stockedLocation, schoolClass, 7));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassStockByCategoryHandler(context).Handle(
            new GetClassStockByCategoryQuery { ClassId = schoolClass.Id, LocationId = emptyLocation.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Labels.ShouldBe(["Storage"]);
        result.Value.Series.Single().Name.ShouldBe("Pantry");
        result.Value.Series.Single().Data.ShouldBe([0]);
    }

    [Test]
    public async Task Handle_WhenClassHasNoBatches_ReturnsEmptyChart()
    {
        await using var context = CreateContext();
        var schoolClass = CreateClass();
        context.SchoolClasses.Add(schoolClass);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassStockByCategoryHandler(context).Handle(
            new GetClassStockByCategoryQuery { ClassId = schoolClass.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Labels.ShouldBeEmpty();
        result.Value.Series.ShouldBeEmpty();
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

    [Test]
    public async Task Handle_WithUnknownLocation_ReturnsLocationNotFoundFailure()
    {
        await using var context = CreateContext();
        var schoolClass = CreateClass();
        context.SchoolClasses.Add(schoolClass);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassStockByCategoryHandler(context).Handle(
            new GetClassStockByCategoryQuery { ClassId = schoolClass.Id, LocationId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Message.ShouldContain("Location");
    }
}
