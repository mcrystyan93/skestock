using FluentValidation.Results;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Stock.Commands.BulkAdjustStock;

public class BulkAdjustStockCommandValidator : AbstractValidator<BulkAdjustStockCommand>
{
    public const int MaxLines = 1000;

    public BulkAdjustStockCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.Reason)
            .IsInEnum()
            .WithErrorCode(ValidationErrorCodes.InvalidEnum);

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.EmptyLines)
            .DependentRules(() =>
            {
                RuleFor(x => x.Lines.Count)
                    .LessThanOrEqualTo(MaxLines)
                    .WithName(nameof(BulkAdjustStockCommand.Lines))
                    .WithErrorCode(ValidationErrorCodes.LessThanOrEqualTo);

                RuleForEach(x => x.Lines).ChildRules(line =>
                {
                    line.RuleFor(l => l.ItemId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
                    line.RuleFor(l => l.LocationId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
                    line.RuleFor(l => l.ActualQuantity)
                        .GreaterThanOrEqualTo(0)
                        .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
                });
            });

        // Existence checks run once per entity set (not per line), and only when the shape is
        // otherwise valid so the sets are bounded by MaxLines.
        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) =>
                await ValidateAsync(dbContext, command, context, cancellationToken))
            .When(x => x.ClassId != Guid.Empty
                       && x.Lines.Count is > 0 and <= MaxLines
                       && x.Lines.All(l => l.ItemId != Guid.Empty && l.LocationId != Guid.Empty));
    }

    private static async Task ValidateAsync(
        IApplicationDbContext dbContext,
        BulkAdjustStockCommand command,
        ValidationContext<BulkAdjustStockCommand> context,
        CancellationToken cancellationToken)
    {
        var seen = new HashSet<(Guid, Guid)>();
        foreach (var line in command.Lines)
        {
            if (!seen.Add((line.ItemId, line.LocationId)))
            {
                context.AddFailure(new ValidationFailure(nameof(command.Lines),
                    "Duplicate line: the same item and location already appear in this count")
                {
                    ErrorCode = ValidationErrorCodes.DuplicateReceiptLine
                });
                break;
            }
        }

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

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var foundItems = await dbContext.Items
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);
        if (foundItems.Count != itemIds.Count)
        {
            context.AddFailure(new ValidationFailure(nameof(command.Lines), "One or more items do not exist")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }

        var locationIds = command.Lines.Select(l => l.LocationId).Distinct().ToList();
        var foundLocations = await dbContext.Locations
            .AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);
        if (foundLocations.Count != locationIds.Count)
        {
            context.AddFailure(new ValidationFailure(nameof(command.Lines), "One or more locations do not exist")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }
    }
}
