using skestock.Application.Common.Errors;

namespace skestock.Application.Storage.Commands.ConfirmUpload;

public class ConfirmUploadCommandValidator : AbstractValidator<ConfirmUploadCommand>
{
    public ConfirmUploadCommandValidator()
    {
        RuleFor(x => x.FileId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
