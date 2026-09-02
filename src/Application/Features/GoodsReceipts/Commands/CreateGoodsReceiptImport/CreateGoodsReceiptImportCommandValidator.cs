using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;

public class CreateGoodsReceiptImportCommandValidator : AbstractValidator<CreateGoodsReceiptImportCommand>
{
    public CreateGoodsReceiptImportCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .DependentRules(() =>
            {
                RuleFor(x => x.ClassId)
                    .MustAsync((classId, cancellationToken) => SchoolClassExistsAsync(dbContext, classId, cancellationToken))
                    .WithMessage("School class does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });

        RuleFor(x => x.FileMetadataId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .DependentRules(() =>
            {
                RuleFor(x => x.FileMetadataId)
                    .MustAsync((fileMetadataId, cancellationToken) => FileMetadataExistsAsync(dbContext, fileMetadataId, cancellationToken))
                    .WithMessage("File metadata does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });
    }

    private static async Task<bool> SchoolClassExistsAsync(IApplicationDbContext dbContext, Guid classId, CancellationToken cancellationToken)
    {
        return await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id == classId, cancellationToken);
    }

    private static async Task<bool> FileMetadataExistsAsync(IApplicationDbContext dbContext, Guid fileMetadataId, CancellationToken cancellationToken)
    {
        return await dbContext.FileMetadata
            .AsNoTracking()
            .AnyAsync(f => f.Id == fileMetadataId, cancellationToken);
    }
}
