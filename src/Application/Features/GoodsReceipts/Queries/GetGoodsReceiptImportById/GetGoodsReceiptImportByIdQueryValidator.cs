using skestock.Application.Common.Errors;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptImportById;

public class GetGoodsReceiptImportByIdQueryValidator : AbstractValidator<GetGoodsReceiptImportByIdQuery>
{
    public GetGoodsReceiptImportByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
