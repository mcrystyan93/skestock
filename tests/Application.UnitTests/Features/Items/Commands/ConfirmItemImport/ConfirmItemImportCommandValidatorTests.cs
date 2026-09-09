using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Commands.ConfirmItemImport;

namespace skestock.Application.UnitTests.Features.Items.Commands.ConfirmItemImport;

public class ConfirmItemImportCommandValidatorTests
{
    private readonly ConfirmItemImportCommandValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorsForValidCommand()
    {
        var result = await _validator.ValidateAsync(new ConfirmItemImportCommand
        {
            ImportId = Guid.NewGuid(),
            Items =
            [
                new ConfirmItemImportItem { Sku = "SKU-1", Name = "Milk", CategoryName = "Dairy", Unit = "L" }
            ]
        });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenImportIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(new ConfirmItemImportCommand
        {
            ImportId = Guid.Empty,
            Items = [new ConfirmItemImportItem { Name = "Milk", CategoryName = "Dairy" }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required && e.PropertyName == "ImportId");
    }

    [Test]
    public async Task ShouldAllowAnEmptyReviewedList()
    {
        var result = await _validator.ValidateAsync(new ConfirmItemImportCommand
        {
            ImportId = Guid.NewGuid(),
            Items = []
        });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenItemNameIsBlank()
    {
        var result = await _validator.ValidateAsync(new ConfirmItemImportCommand
        {
            ImportId = Guid.NewGuid(),
            Items = [new ConfirmItemImportItem { Name = " ", CategoryName = "Dairy" }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required && e.PropertyName == "Items[0].Name");
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenCategoryNameIsBlank()
    {
        var result = await _validator.ValidateAsync(new ConfirmItemImportCommand
        {
            ImportId = Guid.NewGuid(),
            Items = [new ConfirmItemImportItem { Name = "Milk", CategoryName = " " }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required && e.PropertyName == "Items[0].CategoryName");
    }

    [Test]
    public async Task ShouldHaveMaxLengthErrorWhenSkuTooLong()
    {
        var result = await _validator.ValidateAsync(new ConfirmItemImportCommand
        {
            ImportId = Guid.NewGuid(),
            Items = [new ConfirmItemImportItem { Sku = new string('S', 51), Name = "Milk", CategoryName = "Dairy" }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength && e.PropertyName == "Items[0].Sku");
    }
}
