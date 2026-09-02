using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Commands.EditItem;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Commands.EditItem;

public class EditItemCommandValidatorTests
{
    private static async Task<(ItemTestDbContext Context, Category Category, Item Item)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<ItemTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ItemTestDbContext(options);
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        var item = new Item { Name = "Pencil", Unit = "unit", CategoryId = category.Id };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, category, item);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenCommandIsValid()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        var validator = new EditItemCommandValidator(context);

        var result = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "New Name", Unit = "unit", CategoryId = category.Id });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsEmpty()
    {
        var (context, category, _) = await CreateContextAsync();
        await using var _disposable = context;
        var validator = new EditItemCommandValidator(context);

        var result = await validator.ValidateAsync(new EditItemCommand { Id = Guid.Empty, Name = "Pencil", Unit = "unit", CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(EditItemCommand.Id) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldNotHaveDuplicateSkuErrorWhenSkuBelongsToSameItem()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        item.Sku = "SKU-1";
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new EditItemCommandValidator(context);
        var result = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "Pencil", Sku = "SKU-1", Unit = "unit", CategoryId = category.Id });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateSku);
    }

    [Test]
    public async Task ShouldHaveDuplicateSkuErrorWhenSkuBelongsToAnotherItem()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        var otherItem = new Item { Name = "Eraser", Unit = "unit", Sku = "SKU-2", CategoryId = category.Id };
        context.Items.Add(otherItem);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new EditItemCommandValidator(context);
        var result = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "Pencil", Sku = "SKU-2", Unit = "unit", CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateSku);
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenCategoryDoesNotExist()
    {
        var (context, _, item) = await CreateContextAsync();
        await using var _disposable = context;
        var validator = new EditItemCommandValidator(context);

        var result = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "Pencil", Unit = "unit", CategoryId = Guid.NewGuid() });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldHaveErrorWhenMinThresholdIsNegative()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        var validator = new EditItemCommandValidator(context);

        var result = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "Pencil", Unit = "unit", MinThreshold = -5, CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo);
    }
}
