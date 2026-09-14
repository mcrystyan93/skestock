using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Categories.Commands.ConfirmCategoryImportBatch;

namespace skestock.Application.UnitTests.Features.Categories.Commands.ConfirmCategoryImportBatch;

public class ConfirmCategoryImportBatchCommandValidatorTests
{
    private readonly ConfirmCategoryImportBatchCommandValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorsForValidCommand()
    {
        var result = await _validator.ValidateAsync(new ConfirmCategoryImportBatchCommand
        {
            BatchId = Guid.NewGuid(),
            CategoryNames = ["Dairy", "Bakery"]
        });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenImportIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(new ConfirmCategoryImportBatchCommand
        {
            BatchId = Guid.Empty,
            CategoryNames = ["Dairy"]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required && e.PropertyName == "BatchId");
    }

    [Test]
    public async Task ShouldAllowAnEmptyReviewedList()
    {
        var result = await _validator.ValidateAsync(new ConfirmCategoryImportBatchCommand
        {
            BatchId = Guid.NewGuid(),
            CategoryNames = []
        });

        result.IsValid.ShouldBeTrue();
    }
}
