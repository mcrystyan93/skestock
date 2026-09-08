using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Commands.CreateCategoryImport;

public class CreateCategoryImportCommandValidator : AbstractValidator<CreateCategoryImportCommand>
{
    public CreateCategoryImportCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.FileMetadataId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .DependentRules(() =>
            {
                RuleFor(x => x.FileMetadataId)
                    .MustAsync((fileMetadataId, cancellationToken) =>
                        FileMetadataIsConfirmedAsync(dbContext, fileMetadataId, cancellationToken))
                    .WithMessage("File metadata does not exist or is not confirmed")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });
    }

    private static async Task<bool> FileMetadataIsConfirmedAsync(
        IApplicationDbContext dbContext,
        Guid fileMetadataId,
        CancellationToken cancellationToken)
    {
        return await dbContext.FileMetadata
            .AsNoTracking()
            .AnyAsync(f => f.Id == fileMetadataId && f.Status == FileStatus.Completed, cancellationToken);
    }
}
