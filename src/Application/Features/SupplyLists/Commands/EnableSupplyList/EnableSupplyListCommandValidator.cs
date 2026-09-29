using skestock.Application.Common.Errors;

namespace skestock.Application.Features.SupplyLists.Commands.EnableSupplyList;

public class EnableSupplyListCommandValidator : AbstractValidator<EnableSupplyListCommand>
{
    public EnableSupplyListCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
