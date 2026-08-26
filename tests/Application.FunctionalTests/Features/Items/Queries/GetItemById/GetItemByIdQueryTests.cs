using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Items.Queries.GetItemById;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Items.Queries.GetItemById;

public class GetItemByIdQueryTests : TestBase
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
    public async Task Handle_WithExistingId_ReturnsMatchingItemDto()
    {
        var category = await SeedCategoryAsync();
        var item = new Item { Name = $"{_prefix}-Pencil", Sku = $"{_prefix}-SKU", Unit = "unit", CategoryId = category.Id };
        await TestApp.AddAsync(item);

        var result = await TestApp.SendAsync(new GetItemByIdQuery { Id = item.Id });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(item.Id);
        result.Value.Name.ShouldBe(item.Name);
        result.Value.Sku.ShouldBe(item.Sku);
    }

    [Test]
    public async Task Handle_WithItem_ReturnsCategoryNameInDto()
    {
        var category = await SeedCategoryAsync();
        var item = new Item { Name = $"{_prefix}-Pencil", Unit = "unit", CategoryId = category.Id };
        await TestApp.AddAsync(item);

        var result = await TestApp.SendAsync(new GetItemByIdQuery { Id = item.Id });

        result.IsSuccess.ShouldBeTrue();
        result.Value.CategoryId.ShouldBe(category.Id);
        result.Value.CategoryName.ShouldBe(category.Name);
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var result = await TestApp.SendAsync(new GetItemByIdQuery { Id = int.MaxValue });

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithInvalidId_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new GetItemByIdQuery { Id = 0 });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(GetItemByIdQuery.Id));
    }
}
