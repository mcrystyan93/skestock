using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Items.Commands.DisableItem;

public class DisableItemCommandValidator : AbstractValidator<DisableItemCommand>
{
    public DisableItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
