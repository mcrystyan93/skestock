using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.OrderLists.Commands.CreateOrderList;
using skestock.Application.Features.OrderLists.Models;
using skestock.Domain.Entities;

namespace skestock.Application.UnitTests.Features.OrderLists.Commands.CreateOrderList;

public class CreateOrderListCommandValidatorTests
{
    private static async Task<(OrderListTestDbContext Context, Item ActiveItem, Item InactiveItem, SchoolClass Class)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<OrderListTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new OrderListTestDbContext(options);

        var category = new Category { Name = "Pantry" };
        context.Categories.Add(category);

        var activeItem = new Item { Name = "Rice", Unit = "kg", MinThreshold = 10, IsPerishable = false, IsActive = true, Category = category };
        var inactiveItem = new Item { Name = "Sugar", Unit = "kg", MinThreshold = 5, IsPerishable = false, IsActive = false, Category = category };
        context.Items.AddRange(activeItem, inactiveItem);

        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        context.SchoolClasses.Add(schoolClass);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, activeItem, inactiveItem, schoolClass);
    }

    [Test]
    public async Task ShouldNotHaveErrorsForValidCommand()
    {
        var (context, activeItem, _, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateOrderListCommandValidator(context);

        var command = new CreateOrderListCommand
        {
            ClassId = schoolClass.Id,
            Name = "Weekly order",
            Lines =
            [
                new OrderListLineInput { ItemId = activeItem.Id, Quantity = 3, Unit = "kg" },
                new OrderListLineInput { ProductName = "Napkins", Quantity = 2, Unit = "buc", Notes = "any brand" }
            ]
        };

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenClassDoesNotExist()
    {
        var (context, activeItem, _, _) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateOrderListCommandValidator(context);

        var command = new CreateOrderListCommand
        {
            ClassId = Guid.NewGuid(),
            Lines = [new OrderListLineInput { ItemId = activeItem.Id, Quantity = 1 }]
        };

        var result = await validator.ValidateAsync(command);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldHaveErrorWhenQuantityNotPositive()
    {
        var (context, activeItem, _, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateOrderListCommandValidator(context);

        var command = new CreateOrderListCommand
        {
            ClassId = schoolClass.Id,
            Lines = [new OrderListLineInput { ItemId = activeItem.Id, Quantity = 0 }]
        };

        var result = await validator.ValidateAsync(command);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }

    [Test]
    public async Task ShouldHaveErrorWhenUnitMissing()
    {
        var (context, activeItem, _, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateOrderListCommandValidator(context);

        var command = new CreateOrderListCommand
        {
            ClassId = schoolClass.Id,
            Lines = [new OrderListLineInput { ItemId = activeItem.Id, Quantity = 1 }]
        };

        var result = await validator.ValidateAsync(command);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveErrorWhenProductNameMissingAndNoItem()
    {
        var (context, _, _, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateOrderListCommandValidator(context);

        var command = new CreateOrderListCommand
        {
            ClassId = schoolClass.Id,
            Lines = [new OrderListLineInput { Quantity = 1 }]
        };

        var result = await validator.ValidateAsync(command);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveErrorWhenReferencedItemIsInactive()
    {
        var (context, _, inactiveItem, schoolClass) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateOrderListCommandValidator(context);

        var command = new CreateOrderListCommand
        {
            ClassId = schoolClass.Id,
            Lines = [new OrderListLineInput { ItemId = inactiveItem.Id, Quantity = 1 }]
        };

        var result = await validator.ValidateAsync(command);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }
}
