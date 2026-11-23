using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Expenses.Commands.CreateExpenseFromDocument;

public class CreateExpenseFromDocumentCommandValidator : AbstractValidator<CreateExpenseFromDocumentCommand>
{
    public CreateExpenseFromDocumentCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .DependentRules(() =>
            {
                RuleFor(x => x.ClassId)
                    .MustAsync((classId, ct) => dbContext.SchoolClasses.AsNoTracking().AnyAsync(c => c.Id == classId, ct))
                    .WithMessage("School class does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });

        RuleFor(x => x.FileMetadataId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .DependentRules(() =>
            {
                // Completed = the upload was confirmed, so the blob exists and can be read by the worker.
                RuleFor(x => x.FileMetadataId)
                    .MustAsync((id, ct) => dbContext.FileMetadata.AsNoTracking()
                        .AnyAsync(f => f.Id == id && f.Status == FileStatus.Completed, ct))
                    .WithMessage("File does not exist or its upload is not completed")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });
    }
}
