using skestock.Application.Common.Errors;

namespace skestock.Application.Features.FruitDessertPlans.Commands.SetFruitDessertCongregation;

public class SetFruitDessertCongregationCommandValidator : AbstractValidator<SetFruitDessertCongregationCommand>
{
    public SetFruitDessertCongregationCommandValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.EntryId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
    }
}
