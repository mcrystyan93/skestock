using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Commands.CreateItem;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Commands.CreateItem;

public class CreateItemCommandValidatorTests
{
    private static async Task<(ItemTestDbContext Context, Category Category)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<ItemTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ItemTestDbContext(options);
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, category);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenCommandIsValid()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "unit", CategoryId = category.Id });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenNameIsEmpty()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "", Unit = "unit", CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateItemCommand.Name) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenUnitIsEmpty()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "", CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateItemCommand.Unit) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveErrorWhenMinThresholdIsNegative()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "unit", MinThreshold = -1, CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateItemCommand.MinThreshold) && e.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenMinThresholdIsZero()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "unit", MinThreshold = 0, CategoryId = category.Id });

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(CreateItemCommand.MinThreshold));
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenCategoryIdIsEmpty()
    {
        var (context, _) = await CreateContextAsync();
        await using var __ = context;
        var validator = new CreateItemCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "unit", CategoryId = Guid.Empty });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateItemCommand.CategoryId) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenCategoryDoesNotExist()
    {
        var (context, _) = await CreateContextAsync();
        await using var __ = context;
        var validator = new CreateItemCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "unit", CategoryId = Guid.NewGuid() });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldHaveDuplicateSkuErrorWhenSkuAlreadyExists()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        context.Items.Add(new Item { Name = "Pencil", Unit = "unit", Sku = "SKU-1", CategoryId = category.Id });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateItemCommandValidator(context);
        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Other", Unit = "unit", Sku = "SKU-1", CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateSku);
    }

    [Test]
    public async Task ShouldHaveDuplicateSkuErrorWhenSkuDiffersOnlyByCaseOrWhitespace()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        context.Items.Add(new Item { Name = "Pencil", Unit = "unit", Sku = "SKU-1", CategoryId = category.Id });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateItemCommandValidator(context);
        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Other", Unit = "unit", Sku = "  sku-1  ", CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateSku);
    }

    [Test]
    public async Task ShouldNotHaveDuplicateSkuErrorWhenSkuIsNull()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        context.Items.Add(new Item { Name = "Pencil", Unit = "unit", Sku = null, CategoryId = category.Id });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateItemCommandValidator(context);
        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Other", Unit = "unit", CategoryId = category.Id });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateSku);
    }
}
