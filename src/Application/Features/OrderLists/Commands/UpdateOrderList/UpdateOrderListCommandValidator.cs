using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.OrderLists.Commands.UpdateOrderList;

public class UpdateOrderListCommandValidator : AbstractValidator<UpdateOrderListCommand>
{
    private const int NameMaxLength = 200;
    private const int NoteMaxLength = 1000;

    public UpdateOrderListCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.Name)
            .MaximumLength(NameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.Note)
            .MaximumLength(NoteMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        this.AddOrderListLineRules(x => x.Lines, dbContext);
    }
}
