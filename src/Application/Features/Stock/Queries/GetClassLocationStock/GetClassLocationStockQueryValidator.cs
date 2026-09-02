using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Stock.Queries.GetClassLocationStock;

public class GetClassLocationStockQueryValidator : AbstractValidator<GetClassLocationStockQuery>
{
    public GetClassLocationStockQueryValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.LocationId!.Value)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .When(x => x.LocationId is not null);
    }
}
