using FluentValidation.Results;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Stock.Commands.SetClassItemStockVisibility;

public class SetClassItemStockVisibilityCommandValidator
    : AbstractValidator<SetClassItemStockVisibilityCommand>
{
    public SetClassItemStockVisibilityCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) =>
                await ValidateReferencesAsync(dbContext, command, context, cancellationToken))
            .When(x => x.ClassId != Guid.Empty && x.ItemId != Guid.Empty);
    }

    private static async Task ValidateReferencesAsync(
        IApplicationDbContext dbContext,
        SetClassItemStockVisibilityCommand command,
        ValidationContext<SetClassItemStockVisibilityCommand> context,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.SchoolClasses
                .AsNoTracking()
                .AnyAsync(c => c.Id == command.ClassId, cancellationToken))
        {
            context.AddFailure(new ValidationFailure(nameof(command.ClassId), "School class does not exist")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }

        if (!await dbContext.Items
                .AsNoTracking()
                .AnyAsync(i => i.Id == command.ItemId, cancellationToken))
        {
            context.AddFailure(new ValidationFailure(nameof(command.ItemId), "Item does not exist")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }
    }
}
