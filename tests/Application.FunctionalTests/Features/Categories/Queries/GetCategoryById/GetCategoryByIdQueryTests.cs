using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Categories.Queries.GetCategoryById;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQueryTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    [Test]
    public async Task Handle_WithExistingId_ReturnsMatchingCategoryDto()
    {
        var category = new Category
        {
            Name = $"{_prefix}-Stationery",
            Icon = new CategoryIcon("Square Q", "square-q", "/assets/icons/square-q.svg")
        };
        await TestApp.AddAsync(category);

        var result = await TestApp.SendAsync(new GetCategoryByIdQuery { Id = category.Id });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(category.Id);
        result.Value.Name.ShouldBe(category.Name);
        result.Value.Icon.ShouldNotBeNull();
        result.Value.Icon.Name.ShouldBe("Square Q");
        result.Value.Icon.FileName.ShouldBe("square-q");
        result.Value.Icon.Path.ShouldBe("/assets/icons/square-q.svg");
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var result = await TestApp.SendAsync(new GetCategoryByIdQuery { Id = Guid.NewGuid() });

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithInvalidId_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new GetCategoryByIdQuery { Id = Guid.Empty });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(GetCategoryByIdQuery.Id));
    }
}
