using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Categories.Commands.ConfirmCategoryImport;

namespace skestock.Application.UnitTests.Features.Categories.Commands.ConfirmCategoryImport;

public class ConfirmCategoryImportCommandValidatorTests
{
    private readonly ConfirmCategoryImportCommandValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorsForValidCommand()
    {
        var result = await _validator.ValidateAsync(new ConfirmCategoryImportCommand
        {
            ImportId = Guid.NewGuid(),
            CategoryNames = ["Dairy", "Bakery"]
        });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenImportIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(new ConfirmCategoryImportCommand
        {
            ImportId = Guid.Empty,
            CategoryNames = ["Dairy"]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required && e.PropertyName == "ImportId");
    }

    [Test]
    public async Task ShouldAllowAnEmptyReviewedList()
    {
        var result = await _validator.ValidateAsync(new ConfirmCategoryImportCommand
        {
            ImportId = Guid.NewGuid(),
            CategoryNames = []
        });

        result.IsValid.ShouldBeTrue();
    }
}
