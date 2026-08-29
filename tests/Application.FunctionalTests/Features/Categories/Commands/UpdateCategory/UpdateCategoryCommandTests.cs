using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Categories.Commands.CreateCategory;
using skestock.Application.Features.Categories.Commands.UpdateCategory;
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
            Name = $"{_prefix}-New"
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe($"{_prefix}-New");

        var persisted = await TestApp.FindAsync<Category>(created.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe($"{_prefix}-New");
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var result = await TestApp.SendAsync(new UpdateCategoryCommand { Id = int.MaxValue, Name = $"{_prefix}-X" });

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
