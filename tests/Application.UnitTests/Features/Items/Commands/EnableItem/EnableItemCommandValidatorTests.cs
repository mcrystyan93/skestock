using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Commands.EnableItem;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Commands.EnableItem;

public class EnableItemCommandValidatorTests
{
    private readonly EnableItemCommandValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorWhenIdIsNotEmpty()
    {
        var result = await _validator.ValidateAsync(new EnableItemCommand { Id = Guid.NewGuid() });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(new EnableItemCommand { Id = Guid.Empty });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }
}
