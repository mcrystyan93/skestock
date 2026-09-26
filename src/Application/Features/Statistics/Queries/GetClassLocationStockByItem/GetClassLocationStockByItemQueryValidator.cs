using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Statistics.Queries.GetClassLocationStockByItem;

public class GetClassLocationStockByItemQueryValidator : AbstractValidator<GetClassLocationStockByItemQuery>
{
    public GetClassLocationStockByItemQueryValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.LocationId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
