using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Categories.Commands.CreateCategory;
using skestock.Application.Features.Categories.Queries.GetAllCategories;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    [Test]
    public async Task Handle_WithValidName_PersistsCategoryAndReturnsDto()
    {
        var name = $"{_prefix}-Stationery";

        var result = await TestApp.SendAsync(new CreateCategoryCommand { Name = name });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe(name);
        result.Value.Id.ShouldBeGreaterThan(0);

        var persisted = await TestApp.FindAsync<Category>(result.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe(name);
    }

    [Test]
    public async Task Handle_WithDuplicateName_ThrowsValidationException()
    {
        var name = $"{_prefix}-Stationery";
        await TestApp.SendAsync(new CreateCategoryCommand { Name = name });

        var act = async () => await TestApp.SendAsync(new CreateCategoryCommand { Name = name });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateCategoryCommand.Name));
    }

    [Test]
    public async Task Handle_WithEmptyName_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new CreateCategoryCommand { Name = "" });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateCategoryCommand.Name));
    }

    [Test]
    public async Task Handle_OnSuccess_InvalidatesGetAllCategoriesCacheForSameSearchTerm()
    {
        // Prime the cache with an empty page for this test's unique search term.
        var before = await TestApp.SendAsync(new GetAllCategoriesQuery { SearchTerm = _prefix });
        before.Value.Data.Count().ShouldBe(0);

        var name = $"{_prefix}-Electronics";
        await TestApp.SendAsync(new CreateCategoryCommand { Name = name });

        // If CacheInvalidationBehavior didn't invalidate the "categories" tag, this would still
        // return the stale empty page cached above.
        var after = await TestApp.SendAsync(new GetAllCategoriesQuery { SearchTerm = _prefix });
        after.Value.Data.Count().ShouldBe(1);
        after.Value.Data.Single().Name.ShouldBe(name);
    }
}
