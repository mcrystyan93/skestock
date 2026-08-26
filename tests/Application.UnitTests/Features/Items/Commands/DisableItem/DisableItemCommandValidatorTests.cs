using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Commands.DisableItem;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Commands.DisableItem;

public class DisableItemCommandValidatorTests
{
    private readonly DisableItemCommandValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorWhenIdIsGreaterThanZero()
    {
        var result = await _validator.ValidateAsync(new DisableItemCommand { Id = 1 });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsZeroOrNegative()
    {
        var result = await _validator.ValidateAsync(new DisableItemCommand { Id = 0 });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }
}
