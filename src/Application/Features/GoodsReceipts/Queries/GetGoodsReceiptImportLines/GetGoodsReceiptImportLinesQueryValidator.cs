using skestock.Application.Common.Errors;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptImportLines;

public class GetGoodsReceiptImportLinesQueryValidator : AbstractValidator<GetGoodsReceiptImportLinesQuery>
{
    public GetGoodsReceiptImportLinesQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
    }
}
