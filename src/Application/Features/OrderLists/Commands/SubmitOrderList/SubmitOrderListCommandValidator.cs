using skestock.Application.Common.Errors;

namespace skestock.Application.Features.OrderLists.Commands.SubmitOrderList;

public class SubmitOrderListCommandValidator : AbstractValidator<SubmitOrderListCommand>
{
    public SubmitOrderListCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
