using skestock.Application.Common.Errors;

namespace skestock.Application.Features.OrderLists.Commands.ReopenOrderList;

public class ReopenOrderListCommandValidator : AbstractValidator<ReopenOrderListCommand>
{
    public ReopenOrderListCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
