using skestock.Application.Common.Errors;

namespace skestock.Application.Features.FruitDessertPlans.Queries.GetInformarePdf;

public class GetInformarePdfQueryValidator : AbstractValidator<GetInformarePdfQuery>
{
    public GetInformarePdfQueryValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.EntryId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
    }
}
