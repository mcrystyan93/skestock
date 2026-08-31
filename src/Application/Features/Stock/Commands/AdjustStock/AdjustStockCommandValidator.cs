using FluentValidation.Results;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Stock.Commands.AdjustStock;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ClassId)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan);

        RuleFor(x => x.ItemId)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan);

        RuleFor(x => x.LocationId)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan);

        RuleFor(x => x.ActualQuantity)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);

        RuleFor(x => x.Reason)
            .IsInEnum()
            .WithErrorCode(ValidationErrorCodes.InvalidEnum);

        // Bulk (single round-trip) checks: class/item/location existence, plus the no-op
        // (counted quantity == current total) rule. Only run once the individual id fields are
        // valid, matching CreateGoodsReceiptCommandValidator's DependentRules/When style.
        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) =>
                await ValidateAsync(dbContext, command, context, cancellationToken))
            .When(x => x.ClassId > 0 && x.ItemId > 0 && x.LocationId > 0);
    }

    private static async Task ValidateAsync(
        IApplicationDbContext dbContext,
        AdjustStockCommand command,
        ValidationContext<AdjustStockCommand> context,
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

        // Only compute/compare the current total once the referenced entities are known to
        // exist - otherwise the sum is trivially 0 for a nonexistent item/location and would
        // misleadingly report "no adjustment needed" instead of the real InvalidReference errors.
        if (classExists && itemExists && locationExists)
        {
            var currentTotal = await dbContext.StockBatches
                .AsNoTracking()
                .Where(b => b.ItemId == command.ItemId
                            && b.LocationId == command.LocationId
                            && b.ReceivedClassId == command.ClassId)
                .SumAsync(b => b.Quantity, cancellationToken);

            if (currentTotal == command.ActualQuantity)
            {
                context.AddFailure(new ValidationFailure(nameof(command.ActualQuantity),
                    "The counted quantity matches the current stock total - no adjustment is needed")
                {
                    ErrorCode = ValidationErrorCodes.NoAdjustmentNeeded
                });
            }
        }
    }
}
