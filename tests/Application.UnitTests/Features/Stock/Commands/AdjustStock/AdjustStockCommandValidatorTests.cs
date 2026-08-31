using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Stock.Commands.AdjustStock;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Stock.Commands.AdjustStock;

public class AdjustStockCommandValidatorTests
{
    private static async Task<(GoodsReceiptTestDbContext Context, Item Item, Location Location, SchoolClass Class)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new GoodsReceiptTestDbContext(options);

        var category = new Category { Name = "Pantry" };
        context.Categories.Add(category);

        var item = new Item { Name = "Rice", Unit = "kg", MinThreshold = 10, IsPerishable = false, Category = category };
        context.Items.Add(item);

        var location = new Location { Name = "Main Storage", Type = "StorageRoom" };
        context.Locations.Add(location);

        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        context.SchoolClasses.Add(schoolClass);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, item, location, schoolClass);
    }

    [Test]
    public async Task ShouldNotHaveErrorsForValidCommand()
    {
        var (context, item, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;

        context.StockBatches.Add(new StockBatch
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 20,
            ReceivedDate = new DateOnly(2026, 1, 1),
            UnitPrice = 1
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new AdjustStockCommandValidator(context);

        var result = await validator.ValidateAsync(new AdjustStockCommand
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id,
            ActualQuantity = 10,
            Reason = AdjustmentReason.Miscount
        });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenClassDoesNotExist()
    {
        var (context, item, location, _) = await CreateContextAsync();
        await using var _ = context;
        var validator = new AdjustStockCommandValidator(context);

        var result = await validator.ValidateAsync(new AdjustStockCommand
        {
            ClassId = 9999,
            ItemId = item.Id,
            LocationId = location.Id,
            ActualQuantity = 10,
            Reason = AdjustmentReason.Miscount
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == "ClassId");
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenItemDoesNotExist()
    {
        var (context, _, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new AdjustStockCommandValidator(context);

        var result = await validator.ValidateAsync(new AdjustStockCommand
        {
            ClassId = schoolClass.Id,
            ItemId = 9999,
            LocationId = location.Id,
            ActualQuantity = 10,
            Reason = AdjustmentReason.Miscount
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == "ItemId");
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenLocationDoesNotExist()
    {
        var (context, item, _, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new AdjustStockCommandValidator(context);

        var result = await validator.ValidateAsync(new AdjustStockCommand
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = 9999,
            ActualQuantity = 10,
            Reason = AdjustmentReason.Miscount
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == "LocationId");
    }

    [Test]
    public async Task ShouldHaveNoAdjustmentNeededErrorWhenActualQuantityMatchesCurrentTotal()
    {
        var (context, item, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;

        context.StockBatches.Add(new StockBatch
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 15,
            ReceivedDate = new DateOnly(2026, 1, 1),
            UnitPrice = 1
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new AdjustStockCommandValidator(context);

        var result = await validator.ValidateAsync(new AdjustStockCommand
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id,
            ActualQuantity = 15,
            Reason = AdjustmentReason.Miscount
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.NoAdjustmentNeeded);
    }

    [Test]
    public async Task ShouldHaveGreaterThanOrEqualToErrorWhenActualQuantityIsNegative()
    {
        var (context, item, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new AdjustStockCommandValidator(context);

        var result = await validator.ValidateAsync(new AdjustStockCommand
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id,
            ActualQuantity = -1,
            Reason = AdjustmentReason.Miscount
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo && e.PropertyName == "ActualQuantity");
    }
}
