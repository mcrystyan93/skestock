using skestock.Application.Common.Errors;

namespace skestock.Application.Features.OrderLists.Commands.DeleteOrderList;

public class DeleteOrderListCommandValidator : AbstractValidator<DeleteOrderListCommand>
{
    public DeleteOrderListCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
