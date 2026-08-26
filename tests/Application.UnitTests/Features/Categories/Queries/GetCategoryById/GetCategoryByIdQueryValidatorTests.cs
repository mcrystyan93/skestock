using skestock.Application.Common.Errors;
using skestock.Application.Features.Categories.Queries.GetCategoryById;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQueryValidatorTests
{
    private readonly GetCategoryByIdQueryValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorWhenIdIsGreaterThanZero()
    {
        var result = await _validator.ValidateAsync(new GetCategoryByIdQuery { Id = 1 });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsZero()
    {
        var result = await _validator.ValidateAsync(new GetCategoryByIdQuery { Id = 0 });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsNegative()
    {
        var result = await _validator.ValidateAsync(new GetCategoryByIdQuery { Id = -1 });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }
}
