using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Storage.Commands;
using skestock.Application.Storage.Commands.ConfirmUpload;

namespace skestock.Application.UnitTests.Features.Storage.Commands.ConfirmUpload;

public class ConfirmUploadCommandValidatorTests
{
    private readonly ConfirmUploadCommandValidator _validator = new();

    [Test]
    public void Validate_WithEmptyFileId_Fails()
    {
        var result = _validator.Validate(new ConfirmUploadCommand { FileId = Guid.Empty });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ConfirmUploadCommand.FileId)
            && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public void Validate_WithNonEmptyFileId_Passes()
    {
        var result = _validator.Validate(new ConfirmUploadCommand { FileId = Guid.NewGuid() });

        result.IsValid.ShouldBeTrue();
    }
}
