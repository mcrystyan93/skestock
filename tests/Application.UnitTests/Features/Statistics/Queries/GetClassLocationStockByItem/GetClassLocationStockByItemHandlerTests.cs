using Microsoft.EntityFrameworkCore;
using skestock.Application.Features.Statistics.Models;
using skestock.Application.Features.Statistics.Queries.GetClassLocationStockByItem;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetClassLocationStockByItem;

public class GetClassLocationStockByItemHandlerTests
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

    private static Item CreateItem(Category category, string name) =>
        new() { Name = name, Unit = "unit", Category = category };

    private static Location CreateLocation(string name) => new() { Name = name, Type = "StorageRoom" };

    private static StockBatch CreateBatch(Item item, Location location, SchoolClass schoolClass, int quantity) =>
        new()
        {
            Item = item,
            Location = location,
            ReceivedClass = schoolClass,
            Quantity = quantity,
            ReceivedDate = new DateOnly(2026, 9, 1)
        };

    private static Task<FluentResults.Result<ClassStockByCategoryDto>> Handle(
        GoodsReceiptTestDbContext context,
        Guid classId,
        Guid locationId) =>
        new GetClassLocationStockByItemHandler(context).Handle(
            new GetClassLocationStockByItemQuery { ClassId = classId, LocationId = locationId },
            CancellationToken.None).AsTask();

    [Test]
    public async Task Handle_ReturnsOneBarPerItemGroupedByCategoryWithQuantityInOwnCategorySeries()
    {
        await using var context = CreateContext();
        var schoolClass = CreateClass();
        var otherClass = CreateClass("Winter 2027");
        var pantry = CreateCategory("Pantry");
        var cleaning = CreateCategory("Cleaning");
        var rice = CreateItem(pantry, "Rice");
        var beans = CreateItem(pantry, "Beans");
        var soap = CreateItem(cleaning, "Soap");
        var usedUp = CreateItem(cleaning, "Used up");
        var foreign = CreateItem(pantry, "Foreign");
        var kitchen = CreateLocation("Kitchen");
        var storage = CreateLocation("Storage");
        context.AddRange(schoolClass, otherClass, pantry, cleaning, rice, beans, soap, usedUp, foreign, kitchen, storage);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.AddRange(
            CreateBatch(rice, kitchen, schoolClass, 4),
            CreateBatch(rice, kitchen, schoolClass, 1),
            CreateBatch(beans, kitchen, schoolClass, 2),
            CreateBatch(soap, kitchen, schoolClass, -3),
            CreateBatch(usedUp, kitchen, schoolClass, 2),
            CreateBatch(usedUp, kitchen, schoolClass, -2),
            CreateBatch(rice, storage, schoolClass, 9),
            CreateBatch(foreign, kitchen, otherClass, 7));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await Handle(context, schoolClass.Id, kitchen.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Labels.ShouldBe(["Soap", "Beans", "Rice"]);
        result.Value.LabelIds.ShouldBe([soap.Id, beans.Id, rice.Id]);
        result.Value.Series.Select(series => series.Name).ShouldBe(["Cleaning", "Pantry"]);
        result.Value.Series.Single(series => series.Name == "Cleaning").Data.ShouldBe([-3, 0, 0]);
        result.Value.Series.Single(series => series.Name == "Pantry").Data.ShouldBe([0, 2, 5]);
    }

    [Test]
    public async Task Handle_WithLocationWithoutClassStock_ReturnsEmptyChart()
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

        var result = await Handle(context, schoolClass.Id, emptyLocation.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Labels.ShouldBeEmpty();
        result.Value.LabelIds.ShouldBeEmpty();
        result.Value.Series.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_WithUnknownClass_ReturnsSchoolClassNotFoundFailure()
    {
        await using var context = CreateContext();

        var result = await Handle(context, Guid.NewGuid(), Guid.NewGuid());

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

        var result = await Handle(context, schoolClass.Id, Guid.NewGuid());

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Message.ShouldContain("Location");
    }
}
