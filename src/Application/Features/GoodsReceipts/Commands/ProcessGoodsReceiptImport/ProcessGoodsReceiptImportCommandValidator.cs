using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.GoodsReceipts.Commands.ProcessGoodsReceiptImport;

public class ProcessGoodsReceiptImportCommandValidator : AbstractValidator<ProcessGoodsReceiptImportCommand>
{
    public ProcessGoodsReceiptImportCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.GoodsReceiptImportId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .DependentRules(() =>
            {
                RuleFor(x => x.GoodsReceiptImportId)
                    .MustAsync((goodsReceiptImportId, cancellationToken) =>
                        GoodsReceiptImportExistsAsync(dbContext, goodsReceiptImportId, cancellationToken))
                    .WithMessage("Goods receipt import does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });
    }

    private static async Task<bool> GoodsReceiptImportExistsAsync(IApplicationDbContext dbContext,
        Guid goodsReceiptImportId, CancellationToken cancellationToken)
    {
        return await dbContext.GoodsReceiptImports
            .AsNoTracking()
            .AnyAsync(x => x.Id == goodsReceiptImportId, cancellationToken);
    }
}
