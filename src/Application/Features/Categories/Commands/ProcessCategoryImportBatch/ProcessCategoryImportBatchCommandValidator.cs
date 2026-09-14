using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Categories.Commands.ProcessCategoryImportBatch;

public class ProcessCategoryImportBatchCommandValidator : AbstractValidator<ProcessCategoryImportBatchCommand>
{
    public ProcessCategoryImportBatchCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(command => command.CategoryImportBatchId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .DependentRules(() =>
            {
                RuleFor(command => command.CategoryImportBatchId)
                    .MustAsync((batchId, cancellationToken) =>
                        CategoryImportBatchExistsAsync(dbContext, batchId, cancellationToken))
                    .WithMessage("Category import batch does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });
    }

    private static Task<bool> CategoryImportBatchExistsAsync(
        IApplicationDbContext dbContext, Guid batchId, CancellationToken cancellationToken) =>
        dbContext.CategoryImportBatches.AsNoTracking()
            .AnyAsync(batch => batch.Id == batchId, cancellationToken);
}
