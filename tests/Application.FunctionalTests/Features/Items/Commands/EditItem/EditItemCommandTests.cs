using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Items.Commands.CreateItem;
using skestock.Application.Features.Items.Commands.EditItem;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Items.Commands.EditItem;

public class EditItemCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private async Task<Category> SeedCategoryAsync(string suffix = "Category")
    {
        var category = new Category { Name = $"{_prefix}-{suffix}" };
        await TestApp.AddAsync(category);
        return category;
    }

    [Test]
    public async Task Handle_WithShelfLifeDays_UpdatesAndPersistsValue()
    {
        var category = await SeedCategoryAsync();
        var created = await TestApp.SendAsync(new CreateItemCommand
        {
            Name = $"{_prefix}-Cheese",
            Unit = "kg",
            IsPerishable = true,
            ShelfLifeDays = 10,
            CategoryId = category.Id
        });

        var result = await TestApp.SendAsync(new EditItemCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-Cheese",
            Unit = "kg",
            IsPerishable = true,
            ShelfLifeDays = 45,
            CategoryId = category.Id
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShelfLifeDays.ShouldBe(45);

        var persisted = await TestApp.FindAsync<Item>(created.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.ShelfLifeDays.ShouldBe(45);
    }

    [Test]
    public async Task Handle_WithValidChanges_UpdatesItemAndReturnsDto()
    {
        var category = await SeedCategoryAsync();
        var created = await TestApp.SendAsync(new CreateItemCommand { Name = $"{_prefix}-Old", Unit = "unit", CategoryId = category.Id });

        var result = await TestApp.SendAsync(new EditItemCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-New",
            Unit = "box",
            MinThreshold = 3,
            IsPerishable = true,
            CategoryId = category.Id
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe($"{_prefix}-New");
        result.Value.Unit.ShouldBe("box");
        result.Value.MinThreshold.ShouldBe(3);
        result.Value.IsPerishable.ShouldBeTrue();

        var persisted = await TestApp.FindAsync<Item>(created.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe($"{_prefix}-New");
    }

    [Test]
    public async Task Handle_WithNewCategoryId_UpdatesRelationshipAndReturnsNewCategoryName()
    {
        var category = await SeedCategoryAsync("First");
        var otherCategory = await SeedCategoryAsync("Second");
        var created = await TestApp.SendAsync(new CreateItemCommand { Name = $"{_prefix}-Widget", Unit = "unit", CategoryId = category.Id });

        var result = await TestApp.SendAsync(new EditItemCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-Widget",
            Unit = "unit",
            CategoryId = otherCategory.Id
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.CategoryId.ShouldBe(otherCategory.Id);
        result.Value.CategoryName.ShouldBe(otherCategory.Name);
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var category = await SeedCategoryAsync();

        var result = await TestApp.SendAsync(new EditItemCommand
        {
            Id = Guid.NewGuid(),
            Name = $"{_prefix}-X",
            Unit = "unit",
            CategoryId = category.Id
        });

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithDuplicateSkuOfAnotherItem_ThrowsValidationException()
    {
        var category = await SeedCategoryAsync();
        var first = await TestApp.SendAsync(new CreateItemCommand { Name = $"{_prefix}-First", Sku = $"{_prefix}-SKU1", Unit = "unit", CategoryId = category.Id });
        var second = await TestApp.SendAsync(new CreateItemCommand { Name = $"{_prefix}-Second", Sku = $"{_prefix}-SKU2", Unit = "unit", CategoryId = category.Id });

        var act = async () => await TestApp.SendAsync(new EditItemCommand
        {
            Id = second.Value.Id,
            Name = $"{_prefix}-Second",
            Sku = $"{_prefix}-SKU1",
            Unit = "unit",
            CategoryId = category.Id
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(EditItemCommand.Sku));
    }

    [Test]
    public async Task Handle_WithSameSkuOnSameItem_DoesNotThrow()
    {
        var category = await SeedCategoryAsync();
        var created = await TestApp.SendAsync(new CreateItemCommand { Name = $"{_prefix}-Item", Sku = $"{_prefix}-SKU", Unit = "unit", CategoryId = category.Id });

        var result = await TestApp.SendAsync(new EditItemCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-Item",
            Sku = $"{_prefix}-SKU",
            Unit = "box",
            CategoryId = category.Id
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Unit.ShouldBe("box");
    }

    [Test]
    public async Task Handle_WithNonExistentCategoryId_ThrowsValidationException()
    {
        var category = await SeedCategoryAsync();
        var created = await TestApp.SendAsync(new CreateItemCommand { Name = $"{_prefix}-Item", Unit = "unit", CategoryId = category.Id });

        var act = async () => await TestApp.SendAsync(new EditItemCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-Item",
            Unit = "unit",
            CategoryId = Guid.NewGuid()
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(EditItemCommand.CategoryId));
    }
}
