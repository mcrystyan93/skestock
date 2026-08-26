using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Items.Commands.DisableItem;

public class DisableItemCommandValidator : AbstractValidator<DisableItemCommand>
{
    public DisableItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan);
    }
}
