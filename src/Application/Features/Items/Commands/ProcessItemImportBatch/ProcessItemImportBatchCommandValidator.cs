using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Items.Commands.ProcessItemImportBatch;

public class ProcessItemImportBatchCommandValidator : AbstractValidator<ProcessItemImportBatchCommand>
{
    public ProcessItemImportBatchCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ItemImportBatchId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .DependentRules(() =>
            {
                RuleFor(x => x.ItemImportBatchId)
                    .MustAsync((batchId, cancellationToken) =>
                        ItemImportBatchExistsAsync(dbContext, batchId, cancellationToken))
                    .WithMessage("Item import batch does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });
    }

    private static async Task<bool> ItemImportBatchExistsAsync(IApplicationDbContext dbContext,
        Guid batchId, CancellationToken cancellationToken)
    {
        return await dbContext.ItemImportBatches
            .AsNoTracking()
            .AnyAsync(x => x.Id == batchId, cancellationToken);
    }
}
