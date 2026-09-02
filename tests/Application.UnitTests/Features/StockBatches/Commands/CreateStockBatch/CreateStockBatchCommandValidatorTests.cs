using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.StockBatches.Commands.CreateStockBatch;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.StockBatches.Commands.CreateStockBatch;

public class CreateStockBatchCommandValidatorTests
{
    private static async Task<(CreateStockBatchTestDbContext Context, Item Item, Item PerishableItem, Location Location, SchoolClass Class)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<CreateStockBatchTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CreateStockBatchTestDbContext(options);

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
        var (context, item, _, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateStockBatchCommandValidator(context);

        var command = new CreateStockBatchCommand
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 10,
            ReceivedDate = new DateOnly(2026, 9, 5),
            UnitPrice = 2.5m
        };

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveGreaterThanErrorWhenQuantityIsZeroOrNegative()
    {
        var (context, item, _, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateStockBatchCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateStockBatchCommand
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 0,
            ReceivedDate = new DateOnly(2026, 9, 5)
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThan && e.PropertyName == nameof(CreateStockBatchCommand.Quantity));
    }

    [Test]
    public async Task ShouldHaveGreaterThanOrEqualToErrorWhenUnitPriceIsNegative()
    {
        var (context, item, _, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateStockBatchCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateStockBatchCommand
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 1,
            ReceivedDate = new DateOnly(2026, 9, 5),
            UnitPrice = -1
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo && e.PropertyName == nameof(CreateStockBatchCommand.UnitPrice));
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenItemDoesNotExist()
    {
        var (context, _, _, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateStockBatchCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateStockBatchCommand
        {
            ItemId = Guid.NewGuid(),
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 1,
            ReceivedDate = new DateOnly(2026, 9, 5)
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == nameof(CreateStockBatchCommand.ItemId));
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenLocationDoesNotExist()
    {
        var (context, item, _, _, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateStockBatchCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateStockBatchCommand
        {
            ItemId = item.Id,
            LocationId = Guid.NewGuid(),
            ReceivedClassId = schoolClass.Id,
            Quantity = 1,
            ReceivedDate = new DateOnly(2026, 9, 5)
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == nameof(CreateStockBatchCommand.LocationId));
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenClassDoesNotExist()
    {
        var (context, item, _, location, _) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateStockBatchCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateStockBatchCommand
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = Guid.NewGuid(),
            Quantity = 1,
            ReceivedDate = new DateOnly(2026, 9, 5)
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == nameof(CreateStockBatchCommand.ReceivedClassId));
    }

    [Test]
    public async Task ShouldHaveExpiryDateRequiredErrorWhenPerishableItemHasNoExpiry()
    {
        var (context, _, perishableItem, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateStockBatchCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateStockBatchCommand
        {
            ItemId = perishableItem.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 1,
            ReceivedDate = new DateOnly(2026, 9, 5)
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.ExpiryDateRequired);
    }

    [Test]
    public async Task ShouldHaveExpiryDateNotAllowedErrorWhenNonPerishableItemHasExpiry()
    {
        var (context, item, _, location, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateStockBatchCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateStockBatchCommand
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 1,
            ExpiryDate = new DateOnly(2026, 10, 1),
            ReceivedDate = new DateOnly(2026, 9, 5)
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.ExpiryDateNotAllowed);
    }
}
