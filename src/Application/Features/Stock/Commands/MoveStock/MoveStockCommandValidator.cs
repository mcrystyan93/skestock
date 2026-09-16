using FluentValidation.Results;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Stock.Commands.MoveStock;

public class MoveStockCommandValidator : AbstractValidator<MoveStockCommand>
{
    public MoveStockCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.SourceLocationId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.DestinationLocationId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.DestinationLocationId)
            .NotEqual(x => x.SourceLocationId)
            .WithMessage("Source and destination locations must be different")
            .WithErrorCode(ValidationErrorCodes.SelfReference);

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan);

        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) =>
                await ValidateReferencesAndSourceQuantityAsync(dbContext, command, context, cancellationToken))
            .When(x => x.ClassId != Guid.Empty
                       && x.ItemId != Guid.Empty
                       && x.SourceLocationId != Guid.Empty
                       && x.DestinationLocationId != Guid.Empty);
    }

    private static async Task ValidateReferencesAndSourceQuantityAsync(
        IApplicationDbContext dbContext,
        MoveStockCommand command,
        ValidationContext<MoveStockCommand> context,
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

        var sourceLocationExists = await dbContext.Locations
            .AsNoTracking()
            .AnyAsync(l => l.Id == command.SourceLocationId, cancellationToken);
        if (!sourceLocationExists)
        {
            context.AddFailure(new ValidationFailure(nameof(command.SourceLocationId), "Source location does not exist")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }

        var destinationLocationExists = await dbContext.Locations
            .AsNoTracking()
            .AnyAsync(l => l.Id == command.DestinationLocationId, cancellationToken);
        if (!destinationLocationExists)
        {
            context.AddFailure(new ValidationFailure(
                nameof(command.DestinationLocationId),
                "Destination location does not exist") { ErrorCode = ValidationErrorCodes.InvalidReference });
        }

        // Stock batches are class-scoped. Restricting the total to the requested class prevents a
        // transfer from consuming another class's stock when the same item and location are shared.
        if (classExists && itemExists && sourceLocationExists)
        {
            var sourceTotal = await dbContext.StockBatches
                .AsNoTracking()
                .Where(b => b.ItemId == command.ItemId
                            && b.LocationId == command.SourceLocationId
                            && b.ReceivedClassId == command.ClassId)
                .SumAsync(b => b.Quantity, cancellationToken);

            if (command.Quantity > sourceTotal)
            {
                context.AddFailure(new ValidationFailure(
                    nameof(command.Quantity),
                    $"The requested quantity cannot exceed the current source stock total ({sourceTotal})")
                {
                    ErrorCode = ValidationErrorCodes.LessThanOrEqualTo
                });
            }
        }
    }
}
