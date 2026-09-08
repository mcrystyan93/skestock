using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Categories.Commands.CreateCategory;
using skestock.Application.Features.Categories.Commands.UpdateCategory;
using skestock.Application.Features.Categories.Models;
using skestock.Application.Features.Categories.Queries.GetAllCategories;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    [Test]
    public async Task Handle_WithValidChanges_UpdatesCategoryAndReturnsDto()
    {
        var created = await TestApp.SendAsync(new CreateCategoryCommand { Name = $"{_prefix}-Old" });

        var result = await TestApp.SendAsync(new UpdateCategoryCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-New",
            Icon = new CategoryIconDto
            {
                Name = "Square Q",
                FileName = "square-q",
                Path = "/assets/icons/square-q.svg"
            }
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe($"{_prefix}-New");
        result.Value.Icon.ShouldNotBeNull();
        result.Value.Icon.Name.ShouldBe("Square Q");

        var persisted = await TestApp.FindAsync<Category>(created.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe($"{_prefix}-New");
        persisted.Icon.ShouldNotBeNull();
        persisted.Icon.Path.ShouldBe("/assets/icons/square-q.svg");
    }

    [Test]
    public async Task Handle_WithNullIcon_ClearsExistingIcon()
    {
        var created = await TestApp.SendAsync(new CreateCategoryCommand
        {
            Name = $"{_prefix}-WithIcon",
            Icon = new CategoryIconDto
            {
                Name = "Square Q",
                FileName = "square-q",
                Path = "/assets/icons/square-q.svg"
            }
        });

        var result = await TestApp.SendAsync(new UpdateCategoryCommand
        {
            Id = created.Value.Id,
            Name = created.Value.Name,
            Icon = null
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Icon.ShouldBeNull();
        (await TestApp.FindAsync<Category>(created.Value.Id))!.Icon.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var result = await TestApp.SendAsync(new UpdateCategoryCommand { Id = Guid.NewGuid(), Name = $"{_prefix}-X" });

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithDuplicateNameOfAnotherCategory_ThrowsValidationException()
    {
        var first = await TestApp.SendAsync(new CreateCategoryCommand { Name = $"{_prefix}-First" });
        var second = await TestApp.SendAsync(new CreateCategoryCommand { Name = $"{_prefix}-Second" });

        var act = async () => await TestApp.SendAsync(new UpdateCategoryCommand
        {
            Id = second.Value.Id,
            Name = $"{_prefix}-First"
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(UpdateCategoryCommand.Name));
    }

    [Test]
    public async Task Handle_WithSameNameOnSameCategory_DoesNotThrow()
    {
        var created = await TestApp.SendAsync(new CreateCategoryCommand { Name = $"{_prefix}-Same" });

        var result = await TestApp.SendAsync(new UpdateCategoryCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-Same"
        });

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithEmptyName_ThrowsValidationException()
    {
        var created = await TestApp.SendAsync(new CreateCategoryCommand { Name = $"{_prefix}-Empty" });

        var act = async () => await TestApp.SendAsync(new UpdateCategoryCommand { Id = created.Value.Id, Name = "" });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(UpdateCategoryCommand.Name));
    }

    [Test]
    public async Task Handle_OnSuccess_InvalidatesGetAllCategoriesCacheForSameSearchTerm()
    {
        var created = await TestApp.SendAsync(new CreateCategoryCommand { Name = $"{_prefix}-Before" });

        // Prime the cache with the current state for this test's unique search term.
        var before = await TestApp.SendAsync(new GetAllCategoriesQuery { SearchTerm = _prefix });
        before.Value.Data.Count().ShouldBe(1);

        var newName = $"{_prefix}-After";
        await TestApp.SendAsync(new UpdateCategoryCommand { Id = created.Value.Id, Name = newName });

        // If CacheInvalidationBehavior didn't invalidate the "categories" tag, this would still
        // return the stale cached page with the old name.
        var after = await TestApp.SendAsync(new GetAllCategoriesQuery { SearchTerm = _prefix });
        after.Value.Data.Single().Name.ShouldBe(newName);
    }
}
