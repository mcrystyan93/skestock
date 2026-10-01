using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.OrderLists.Commands.AddItemToOrderList;
using skestock.Domain.Entities;

namespace skestock.Application.UnitTests.Features.OrderLists.Commands.AddItemToOrderList;

public class AddItemToOrderListCommandValidatorTests
{
    [TestCase(false, null, false)]
    [TestCase(true, null, true)]
    [TestCase(false, "Nume", true)]
    [TestCase(false, "  ", false)]
    [TestCase(true, "Nume", false)]
    public async Task Validate_RequiresExactlyOneTarget(bool useExistingList, string? name, bool valid)
    {
        var (validator, command) = await CreateAsync(useExistingList, name, 1);

        (await validator.ValidateAsync(command)).IsValid.ShouldBe(valid);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task Validate_RejectsNonPositiveQuantity(decimal quantity)
    {
        var (validator, command) = await CreateAsync(true, null, quantity);

        (await validator.ValidateAsync(command)).IsValid.ShouldBeFalse();
    }

    private static async Task<(AddItemToOrderListCommandValidator, AddItemToOrderListCommand)> CreateAsync(
        bool useExistingList, string? name, decimal quantity)
    {
        var options = new DbContextOptionsBuilder<OrderListTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new OrderListTestDbContext(options);
        var category = new Category { Name = "Dairy" };
        var item = new Item { Name = "Milk", Unit = "l", Category = category, IsActive = true };
        var schoolClass = new SchoolClass { Name = "C", StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 20) };
        context.Categories.Add(category);
        context.Items.Add(item);
        context.SchoolClasses.Add(schoolClass);
        await context.SaveChangesAsync();

        return (new AddItemToOrderListCommandValidator(context), new AddItemToOrderListCommand
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            Quantity = quantity,
            OrderListId = useExistingList ? Guid.NewGuid() : null,
            NewOrderListName = name
        });
    }
}
