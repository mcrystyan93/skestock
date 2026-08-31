using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Stock.Queries.GetClassLocationStock;

public class GetClassLocationStockQueryValidator : AbstractValidator<GetClassLocationStockQuery>
{
    public GetClassLocationStockQueryValidator()
    {
        RuleFor(x => x.ClassId)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan);

        RuleFor(x => x.LocationId!.Value)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan)
            .When(x => x.LocationId is not null);
    }
}
