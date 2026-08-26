using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Items.Commands.CreateItem;
using skestock.Application.Features.Items.Commands.DisableItem;
using skestock.Application.Features.Items.Commands.EnableItem;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Items.Commands.EnableItem;

public class EnableItemCommandTests : TestBase
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
    public async Task Handle_WithDisabledItem_SetsIsActiveTrue()
    {
        var category = await SeedCategoryAsync();
        var created = await TestApp.SendAsync(new CreateItemCommand { Name = $"{_prefix}-Item", Unit = "unit", CategoryId = category.Id });
        await TestApp.SendAsync(new DisableItemCommand { Id = created.Value.Id });

        var result = await TestApp.SendAsync(new EnableItemCommand { Id = created.Value.Id });

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeTrue();

        var persisted = await TestApp.FindAsync<Item>(created.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithAlreadyActiveItem_IsIdempotentAndSucceeds()
    {
        var category = await SeedCategoryAsync();
        var created = await TestApp.SendAsync(new CreateItemCommand { Name = $"{_prefix}-Item", Unit = "unit", CategoryId = category.Id });

        var result = await TestApp.SendAsync(new EnableItemCommand { Id = created.Value.Id });

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var result = await TestApp.SendAsync(new EnableItemCommand { Id = int.MaxValue });

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithInvalidId_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new EnableItemCommand { Id = 0 });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(EnableItemCommand.Id));
    }
}
