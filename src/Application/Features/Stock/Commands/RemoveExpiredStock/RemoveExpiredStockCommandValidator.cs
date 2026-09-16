using FluentValidation.Results;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Stock.Commands.RemoveExpiredStock;

public class RemoveExpiredStockCommandValidator : AbstractValidator<RemoveExpiredStockCommand>
{
    public RemoveExpiredStockCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.LocationId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) =>
                await ValidateReferencesAsync(dbContext, command, context, cancellationToken))
            .When(x => x.ClassId != Guid.Empty
                       && x.ItemId != Guid.Empty
                       && x.LocationId != Guid.Empty);
    }

    private static async Task ValidateReferencesAsync(
        IApplicationDbContext dbContext,
        RemoveExpiredStockCommand command,
        ValidationContext<RemoveExpiredStockCommand> context,
        CancellationToken cancellationToken)
    {
        var classExists = await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id == command.ClassId, cancellationToken);
        if (!classExists)
        {
            context.AddFailure(new ValidationFailure(nameof(command.ClassId), "School class does not exist")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }

        var itemExists = await dbContext.Items
            .AsNoTracking()
            .AnyAsync(i => i.Id == command.ItemId, cancellationToken);
        if (!itemExists)
        {
            context.AddFailure(new ValidationFailure(nameof(command.ItemId), "Item does not exist")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }

        var locationExists = await dbContext.Locations
            .AsNoTracking()
            .AnyAsync(l => l.Id == command.LocationId, cancellationToken);
        if (!locationExists)
        {
            context.AddFailure(new ValidationFailure(nameof(command.LocationId), "Location does not exist")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }
    }
}
