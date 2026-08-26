using skestock.Application.Common.Errors;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptById;

public class GetGoodsReceiptByIdQueryValidator : AbstractValidator<GetGoodsReceiptByIdQuery>
{
    public GetGoodsReceiptByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan);
    }
}
