using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.OrderLists.Commands.AddItemToOrderList;

public class AddItemToOrderListCommandValidator : AbstractValidator<AddItemToOrderListCommand>
{
    private const int NameMaxLength = 200;

    public AddItemToOrderListCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MustAsync((classId, ct) => dbContext.SchoolClasses.AsNoTracking().AnyAsync(c => c.Id == classId, ct))
            .When(x => x.ClassId != Guid.Empty)
            .WithMessage("The referenced school class does not exist")
            .WithErrorCode(ValidationErrorCodes.InvalidReference);

        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MustAsync((itemId, ct) => dbContext.Items.AsNoTracking().AnyAsync(i => i.Id == itemId && i.IsActive, ct))
            .When(x => x.ItemId != Guid.Empty)
            .WithMessage("The referenced item does not exist or is not active")
            .WithErrorCode(ValidationErrorCodes.InvalidReference);

        RuleFor(x => x.Quantity).GreaterThan(0).WithErrorCode(ValidationErrorCodes.GreaterThan);

        RuleFor(x => x.NewOrderListName)
            .MaximumLength(NameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x)
            .Must(x => x.OrderListId.HasValue ^ !string.IsNullOrWhiteSpace(x.NewOrderListName))
            .WithName(nameof(AddItemToOrderListCommand.OrderListId))
            .WithMessage("Provide either an existing order list or a name for a new one")
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
