using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Stock.Commands.SetClassItemStockVisibility;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Stock.Commands.SetClassItemStockVisibility;

public class SetClassItemStockVisibilityCommandTests
{
    private static async Task<(GoodsReceiptTestDbContext Context, Item Item, SchoolClass Class, Location Location)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new GoodsReceiptTestDbContext(options);

        var category = new Category { Name = "Pantry" };
        var item = new Item { Name = "Rice", Unit = "kg", Category = category };
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        var location = new Location { Name = "Main Kitchen", Type = "Kitchen" };

        context.Categories.Add(category);
        context.Items.Add(item);
        context.SchoolClasses.Add(schoolClass);
        context.Locations.Add(location);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, item, schoolClass, location);
    }

    [Test]
    public async Task Validator_ValidatesExistingClassAndItemReferences()
    {
        var (context, item, schoolClass, location) = await CreateContextAsync();
        await using var _ = context;

        var result = await new SetClassItemStockVisibilityCommandValidator(context).ValidateAsync(
            new SetClassItemStockVisibilityCommand
            {
                ClassId = schoolClass.Id,
                ItemId = item.Id,
                LocationId = location.Id,
                HideWhenZeroStock = true
            });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task Validator_ReportsMissingClassAndItemReferences()
    {
        var (context, _, _, _) = await CreateContextAsync();
        await using var _ = context;

        var result = await new SetClassItemStockVisibilityCommandValidator(context).ValidateAsync(
            new SetClassItemStockVisibilityCommand
            {
                ClassId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                LocationId = Guid.NewGuid()
            });

        result.Errors.Count(error => error.ErrorCode == ValidationErrorCodes.InvalidReference)
            .ShouldBe(3);
    }

    [Test]
    public async Task Handler_UpsertsVisibilityAndDoesNotCreateDuplicates()
    {
        var (context, item, schoolClass, location) = await CreateContextAsync();
        await using var _ = context;
        var handler = new SetClassItemStockVisibilityCommandHandler(context);

        var enableResult = await handler.Handle(
            new SetClassItemStockVisibilityCommand
            {
                ClassId = schoolClass.Id,
                ItemId = item.Id,
                LocationId = location.Id,
                HideWhenZeroStock = true
            },
            CancellationToken.None);
        var disableResult = await handler.Handle(
            new SetClassItemStockVisibilityCommand
            {
                ClassId = schoolClass.Id,
                ItemId = item.Id,
                LocationId = location.Id,
                HideWhenZeroStock = false
            },
            CancellationToken.None);

        enableResult.IsSuccess.ShouldBeTrue();
        disableResult.IsSuccess.ShouldBeTrue();
        (await context.ClassItemStockVisibilities.CountAsync(CancellationToken.None)).ShouldBe(1);
        (await context.ClassItemStockVisibilities.SingleAsync(CancellationToken.None))
            .HideWhenZeroStock.ShouldBeFalse();
    }

    [Test]
    public async Task Handler_DisablingMissingVisibilityIsNoOp()
    {
        var (context, item, schoolClass, location) = await CreateContextAsync();
        await using var _ = context;

        var result = await new SetClassItemStockVisibilityCommandHandler(context).Handle(
            new SetClassItemStockVisibilityCommand
            {
                ClassId = schoolClass.Id,
                ItemId = item.Id,
                LocationId = location.Id,
                HideWhenZeroStock = false
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await context.ClassItemStockVisibilities.CountAsync(CancellationToken.None)).ShouldBe(0);
    }

    [Test]
    public async Task Handler_UpsertsVisibilitySeparatelyPerLocation()
    {
        var (context, item, schoolClass, location) = await CreateContextAsync();
        await using var _ = context;
        var otherLocation = new Location { Name = "Storage Room", Type = "StorageRoom" };
        context.Locations.Add(otherLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SetClassItemStockVisibilityCommandHandler(context);
        var firstResult = await handler.Handle(
            new SetClassItemStockVisibilityCommand
            {
                ClassId = schoolClass.Id,
                ItemId = item.Id,
                LocationId = location.Id,
                HideWhenZeroStock = true
            },
            CancellationToken.None);
        var secondResult = await handler.Handle(
            new SetClassItemStockVisibilityCommand
            {
                ClassId = schoolClass.Id,
                ItemId = item.Id,
                LocationId = otherLocation.Id,
                HideWhenZeroStock = true
            },
            CancellationToken.None);

        firstResult.IsSuccess.ShouldBeTrue();
        secondResult.IsSuccess.ShouldBeTrue();
        (await context.ClassItemStockVisibilities.CountAsync(CancellationToken.None)).ShouldBe(2);
        (await context.ClassItemStockVisibilities
                .Select(visibility => visibility.LocationId)
                .ToListAsync(CancellationToken.None))
            .ShouldBe([location.Id, otherLocation.Id], ignoreOrder: true);
    }
}
