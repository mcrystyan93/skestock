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
            new ConfirmItemImportItem { ItemId = Guid.NewGuid(), Sku = "SKU-1", Name = "Milk", Unit = "L" }
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
            Items = [new ConfirmItemImportItem { Name = "Milk" }]
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
            Items = [new ConfirmItemImportItem { ItemId = Guid.NewGuid(), Name = " " }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required && e.PropertyName == "Items[0].Name");
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenItemIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(new ConfirmItemImportCommand
        {
            ImportId = Guid.NewGuid(),
            Items = [new ConfirmItemImportItem { Name = "Milk" }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required && e.PropertyName == "Items[0].ItemId");
    }

    [Test]
    public async Task ShouldHaveMaxLengthErrorWhenSkuTooLong()
    {
        var result = await _validator.ValidateAsync(new ConfirmItemImportCommand
        {
            ImportId = Guid.NewGuid(),
            Items = [new ConfirmItemImportItem { ItemId = Guid.NewGuid(), Sku = new string('S', 51), Name = "Milk" }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength && e.PropertyName == "Items[0].Sku");
    }
}
