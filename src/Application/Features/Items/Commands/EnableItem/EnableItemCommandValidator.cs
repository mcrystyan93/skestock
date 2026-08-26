using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Items.Commands.EnableItem;

public class EnableItemCommandValidator : AbstractValidator<EnableItemCommand>
{
    public EnableItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan);
    }
}
