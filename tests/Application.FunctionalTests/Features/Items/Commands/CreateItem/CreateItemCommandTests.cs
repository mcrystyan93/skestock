using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Items.Commands.CreateItem;
using skestock.Application.Features.Items.Queries.GetAllItems;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Items.Commands.CreateItem;

public class CreateItemCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private async Task<Category> SeedCategoryAsync()
    {
        var category = new Category { Name = $"{_prefix}-Category" };
        await TestApp.AddAsync(category);
        return category;
    }

    [Test]
    public async Task Handle_WithValidData_PersistsItemAndReturnsDto()
    {
        var category = await SeedCategoryAsync();
        var name = $"{_prefix}-Pencil";

        var result = await TestApp.SendAsync(new CreateItemCommand
        {
            Name = name,
            Sku = $"{_prefix}-SKU",
            Unit = "box",
            MinThreshold = 5,
            IsPerishable = false,
            CategoryId = category.Id
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe(name);
        result.Value.Unit.ShouldBe("box");
        result.Value.CategoryId.ShouldBe(category.Id);
        result.Value.CategoryName.ShouldBe(category.Name);
        result.Value.IsActive.ShouldBeTrue();
        result.Value.Id.ShouldNotBe(Guid.Empty);

        var persisted = await TestApp.FindAsync<Item>(result.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe(name);
        persisted.CategoryId.ShouldBe(category.Id);
    }

    [Test]
    public async Task Handle_WithDuplicateSku_ThrowsValidationException()
    {
        var category = await SeedCategoryAsync();
        var sku = $"{_prefix}-SKU";
        await TestApp.SendAsync(new CreateItemCommand { Name = $"{_prefix}-First", Sku = sku, Unit = "unit", CategoryId = category.Id });

        var act = async () => await TestApp.SendAsync(new CreateItemCommand { Name = $"{_prefix}-Second", Sku = sku, Unit = "unit", CategoryId = category.Id });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateItemCommand.Sku));
    }

    [Test]
    public async Task Handle_WithEmptyName_ThrowsValidationException()
    {
        var category = await SeedCategoryAsync();

        var act = async () => await TestApp.SendAsync(new CreateItemCommand { Name = "", Unit = "unit", CategoryId = category.Id });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateItemCommand.Name));
    }

    [Test]
    public async Task Handle_WithEmptyUnit_ThrowsValidationException()
    {
        var category = await SeedCategoryAsync();

        var act = async () => await TestApp.SendAsync(new CreateItemCommand { Name = $"{_prefix}-X", Unit = "", CategoryId = category.Id });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateItemCommand.Unit));
    }

    [Test]
    public async Task Handle_WithNonExistentCategoryId_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new CreateItemCommand
        {
            Name = $"{_prefix}-X",
            Unit = "unit",
            CategoryId = Guid.NewGuid()
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateItemCommand.CategoryId));
    }

    [Test]
    public async Task Handle_WithNegativeMinThreshold_ThrowsValidationException()
    {
        var category = await SeedCategoryAsync();

        var act = async () => await TestApp.SendAsync(new CreateItemCommand
        {
            Name = $"{_prefix}-X",
            Unit = "unit",
            MinThreshold = -1,
            CategoryId = category.Id
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateItemCommand.MinThreshold));
    }

    [Test]
    public async Task Handle_OnSuccess_InvalidatesGetAllItemsCacheForSameSearchTerm()
    {
        var category = await SeedCategoryAsync();

        var before = await TestApp.SendAsync(new GetAllItemsQuery { SearchTerm = _prefix });
        before.Value.Data.Count().ShouldBe(0);

        var name = $"{_prefix}-Widget";
        await TestApp.SendAsync(new CreateItemCommand { Name = name, Unit = "unit", CategoryId = category.Id });

        var after = await TestApp.SendAsync(new GetAllItemsQuery { SearchTerm = _prefix });
        after.Value.Data.Count().ShouldBe(1);
        after.Value.Data.Single().Name.ShouldBe(name);
    }
}
