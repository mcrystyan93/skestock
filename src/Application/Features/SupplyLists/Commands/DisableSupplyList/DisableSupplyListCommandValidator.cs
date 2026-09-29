using skestock.Application.Common.Errors;

namespace skestock.Application.Features.SupplyLists.Commands.DisableSupplyList;

public class DisableSupplyListCommandValidator : AbstractValidator<DisableSupplyListCommand>
{
    public DisableSupplyListCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
