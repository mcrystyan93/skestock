using FluentValidation.Results;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.StockBatches.Commands.CreateStockBatch;

public class CreateStockBatchCommandValidator : AbstractValidator<CreateStockBatchCommand>
{
    public CreateStockBatchCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.LocationId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.ReceivedClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan);

        // Zero is allowed (e.g. donated/free items); negative prices are rejected. Matches
        // CreateGoodsReceiptCommandValidator's per-line UnitPrice rule.
        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);

        // Bulk (single round-trip) checks: item/location/class existence, plus the
        // expiry-date-vs-IsPerishable rule reused from CreateGoodsReceiptCommandValidator. Only
        // run once the individual id fields are valid, matching that validator's When style.
        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) =>
                await ValidateAsync(dbContext, command, context, cancellationToken))
            .When(x => x.ItemId != Guid.Empty && x.LocationId != Guid.Empty && x.ReceivedClassId != Guid.Empty);
    }

    private static async Task ValidateAsync(
        IApplicationDbContext dbContext,
        CreateStockBatchCommand command,
        ValidationContext<CreateStockBatchCommand> context,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.Id == command.ItemId)
            .Select(i => new { i.IsPerishable })
            .SingleOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            context.AddFailure(new ValidationFailure(nameof(command.ItemId), "Item does not exist")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }
        else if (item.IsPerishable && command.ExpiryDate is null)
        {
            context.AddFailure(new ValidationFailure(nameof(command.ExpiryDate), "Expiry date is required for perishable items")
            {
                ErrorCode = ValidationErrorCodes.ExpiryDateRequired
            });
        }
        else if (!item.IsPerishable && command.ExpiryDate is not null)
        {
            context.AddFailure(new ValidationFailure(nameof(command.ExpiryDate), "Expiry date is not allowed for non-perishable items")
            {
                ErrorCode = ValidationErrorCodes.ExpiryDateNotAllowed
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

        var classExists = await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id == command.ReceivedClassId, cancellationToken);
        if (!classExists)
        {
            context.AddFailure(new ValidationFailure(nameof(command.ReceivedClassId), "School class does not exist")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }
    }
}
