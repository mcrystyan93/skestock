using skestock.Application.Common.Errors;

namespace skestock.Application.Storage.Queries.GetFileDownload;

public class GetFileDownloadQueryValidator : AbstractValidator<GetFileDownloadQuery>
{
    public GetFileDownloadQueryValidator()
    {
        RuleFor(x => x.FileId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
