using skestock.Application.Common.Errors;

namespace skestock.Application.Storage.Commands.RequestUpload;

public class RequestUploadCommandValidator : AbstractValidator<RequestUploadCommand>
{
    private const int FileNameMaxLength = 260;
    private const int ContentTypeMaxLength = 128;

    public RequestUploadCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(FileNameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(ContentTypeMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);
    }
}
