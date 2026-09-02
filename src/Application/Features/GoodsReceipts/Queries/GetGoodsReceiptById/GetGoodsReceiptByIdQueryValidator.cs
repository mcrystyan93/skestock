using skestock.Application.Common.Errors;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptById;

public class GetGoodsReceiptByIdQueryValidator : AbstractValidator<GetGoodsReceiptByIdQuery>
{
    public GetGoodsReceiptByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
