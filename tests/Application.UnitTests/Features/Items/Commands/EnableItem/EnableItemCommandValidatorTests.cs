using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Commands.EnableItem;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Commands.EnableItem;

public class EnableItemCommandValidatorTests
{
    private readonly EnableItemCommandValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorWhenIdIsGreaterThanZero()
    {
        var result = await _validator.ValidateAsync(new EnableItemCommand { Id = 1 });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsZeroOrNegative()
    {
        var result = await _validator.ValidateAsync(new EnableItemCommand { Id = 0 });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }
}
