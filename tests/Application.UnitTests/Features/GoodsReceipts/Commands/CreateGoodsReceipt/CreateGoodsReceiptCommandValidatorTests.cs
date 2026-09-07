using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;

public class CreateGoodsReceiptCommandValidatorTests
{
    private static async Task<(GoodsReceiptTestDbContext Context, Item Item, Item PerishableItem, Location Location, SchoolClass Class)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new GoodsReceiptTestDbContext(options);

        var category = new Category { Name = "Pantry" };
        context.Categories.Add(category);

        var item = new Item { Name = "Rice", Unit = "kg", MinThreshold = 10, IsPerishable = false, Category = category };
        var perishableItem = new Item { Name = "Milk", Unit = "L", MinThreshold = 5, IsPerishable = true, Category = category };
        context.Items.AddRange(item, perishableItem);

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

        return (context, item, perishableItem, location, schoolClass);
    }

    [Test]
    public async Task ShouldNotHaveErrorsForValidCommand()
    {
        var (context, item, perishableItem, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptCommandValidator(context);

        var command = new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Weekly delivery",
            Lines =
            [
                new CreateGoodsReceiptLine { ItemId = item.Id, LocationId = location.Id, Quantity = 10 },
                new CreateGoodsReceiptLine { ItemId = perishableItem.Id, LocationId = location.Id, Quantity = 5, ExpiryDate = new DateOnly(2026, 10, 1) }
            ]
        };

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveEmptyLinesErrorWhenNoLinesProvided()
    {
        var (context, _, _, _, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Empty receipt",
            Lines = []
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.EmptyLines);
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenClassDoesNotExist()
    {
        var (context, item, _, location, _) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptCommand
        {
            ClassId = Guid.NewGuid(),
            Note = "Delivery",
            Lines = [new CreateGoodsReceiptLine { ItemId = item.Id, LocationId = location.Id, Quantity = 1 }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == "ClassId");
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenItemDoesNotExist()
    {
        var (context, _, _, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Delivery",
            Lines = [new CreateGoodsReceiptLine { ItemId = Guid.NewGuid(), LocationId = location.Id, Quantity = 1 }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == "Lines[0].ItemId");
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenLocationDoesNotExist()
    {
        var (context, item, _, _, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Delivery",
            Lines = [new CreateGoodsReceiptLine { ItemId = item.Id, LocationId = Guid.NewGuid(), Quantity = 1 }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == "Lines[0].LocationId");
    }

    [Test]
    public async Task ShouldHaveExpiryDateRequiredErrorWhenPerishableItemLineHasNoExpiry()
    {
        var (context, _, perishableItem, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Delivery",
            Lines = [new CreateGoodsReceiptLine { ItemId = perishableItem.Id, LocationId = location.Id, Quantity = 1 }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.ExpiryDateRequired);
    }

    [Test]
    public async Task ShouldNotHaveExpiryDateRequiredErrorWhenPerishableItemHasShelfLifeAndNoExpiry()
    {
        var (context, _, _, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;

        var categoryId = await context.Categories.Select(c => c.Id).FirstAsync(CancellationToken.None);
        var shelfLifeItem = new Item { Name = "Bread", Unit = "loaf", IsPerishable = true, ShelfLifeDays = 3, CategoryId = categoryId };
        context.Items.Add(shelfLifeItem);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateGoodsReceiptCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Delivery",
            Lines = [new CreateGoodsReceiptLine { ItemId = shelfLifeItem.Id, LocationId = location.Id, Quantity = 1 }]
        });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.ExpiryDateRequired);
    }

    [Test]
    public async Task ShouldHaveExpiryDateNotAllowedErrorWhenNonPerishableItemLineHasExpiry()
    {
        var (context, item, _, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Delivery",
            Lines = [new CreateGoodsReceiptLine { ItemId = item.Id, LocationId = location.Id, Quantity = 1, ExpiryDate = new DateOnly(2026, 10, 1) }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.ExpiryDateNotAllowed);
    }

    [Test]
    public async Task ShouldHaveDuplicateReceiptLineErrorWhenSameItemLocationAndExpiryRepeat()
    {
        var (context, item, _, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Duplicate scan",
            Lines =
            [
                new CreateGoodsReceiptLine { ItemId = item.Id, LocationId = location.Id, Quantity = 10 },
                new CreateGoodsReceiptLine { ItemId = item.Id, LocationId = location.Id, Quantity = 5 }
            ]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateReceiptLine);
    }

    [Test]
    public async Task ShouldHaveEmptyLinesErrorWhenLineCountExceedsMax()
    {
        var (context, item, _, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptCommandValidator(context);

        var lines = Enumerable.Range(0, 201)
            .Select(i => new CreateGoodsReceiptLine { ItemId = item.Id, LocationId = location.Id, Quantity = 1, ExpiryDate = null })
            .ToList();

        // Vary expiry-free (ItemId, LocationId) pairs won't be unique across 201 identical lines,
        // but the count check must fire regardless of duplicate-line detection.
        var result = await validator.ValidateAsync(new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Too many lines",
            Lines = lines
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength);
    }
}
