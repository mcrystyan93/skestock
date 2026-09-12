using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Statistics.Queries.GetClassGoodsReceiptCosts;

public class GetClassGoodsReceiptCostsQueryValidator : AbstractValidator<GetClassGoodsReceiptCostsQuery>
{
    public GetClassGoodsReceiptCostsQueryValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithErrorCode(ValidationErrorCodes.InvalidDateRange);
    }
}
