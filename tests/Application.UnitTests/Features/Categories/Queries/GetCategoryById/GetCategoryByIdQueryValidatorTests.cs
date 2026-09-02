using skestock.Application.Common.Errors;
using skestock.Application.Features.Categories.Queries.GetCategoryById;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQueryValidatorTests
{
    private readonly GetCategoryByIdQueryValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorWhenIdIsNotEmpty()
    {
        var result = await _validator.ValidateAsync(new GetCategoryByIdQuery { Id = Guid.NewGuid() });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(new GetCategoryByIdQuery { Id = Guid.Empty });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsEmptyVariant()
    {
        var result = await _validator.ValidateAsync(new GetCategoryByIdQuery { Id = Guid.Empty });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }
}
