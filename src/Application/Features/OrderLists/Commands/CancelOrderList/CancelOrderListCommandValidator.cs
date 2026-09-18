using skestock.Application.Common.Errors;

namespace skestock.Application.Features.OrderLists.Commands.CancelOrderList;

public class CancelOrderListCommandValidator : AbstractValidator<CancelOrderListCommand>
{
    public CancelOrderListCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
