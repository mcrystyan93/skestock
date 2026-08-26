using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Queries.GetItemById;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Queries.GetItemById;

public class GetItemByIdQueryValidatorTests
{
    private readonly GetItemByIdQueryValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorWhenIdIsGreaterThanZero()
    {
        var result = await _validator.ValidateAsync(new GetItemByIdQuery { Id = 1 });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsZeroOrNegative()
    {
        var result = await _validator.ValidateAsync(new GetItemByIdQuery { Id = 0 });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }
}
