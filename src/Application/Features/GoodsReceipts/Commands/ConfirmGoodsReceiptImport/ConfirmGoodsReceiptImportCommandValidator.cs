using System.Text.Json;
using FluentValidation.Results;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.GoodsReceipts.Models;

namespace skestock.Application.Features.GoodsReceipts.Commands.ConfirmGoodsReceiptImport;

public class ConfirmGoodsReceiptImportCommandValidator : AbstractValidator<ConfirmGoodsReceiptImportCommand>
{
    // Keep in sync with GoodsReceiptConfiguration's HasMaxLength.
    private const int NoteMaxLength = 500;
    private const int SupplierReferenceMaxLength = 200;
    private const int MaxLines = 200;

    public ConfirmGoodsReceiptImportCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ImportId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.Note)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(NoteMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.SupplierReference)
            .MaximumLength(SupplierReferenceMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.EmptyLines)
            .WithMessage("A goods receipt import confirmation must have at least one line")
            .Must(lines => lines.Count <= MaxLines)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage($"A goods receipt cannot have more than {MaxLines} lines");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.LocationId)
                .NotEmpty()
                .WithErrorCode(ValidationErrorCodes.Required);

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0)
                .WithErrorCode(ValidationErrorCodes.GreaterThan);

            line.RuleFor(l => l.UnitPrice)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);

            // New-item lines (no ItemId) must carry the fields needed to create the item.
            line.RuleFor(l => l.Name)
                .NotEmpty()
                .WithErrorCode(ValidationErrorCodes.Required)
                .When(l => l.ItemId is null);

            line.RuleFor(l => l.CategoryName)
                .NotEmpty()
                .WithErrorCode(ValidationErrorCodes.Required)
                .When(l => l.ItemId is null);
        });

        // Single round-trip cross-line checks: item/location existence, expiry-vs-perishable, and the
        // split-quantity rule (each source line's split rows must sum back to the extracted quantity).
        RuleFor(x => x)
            .CustomAsync((command, context, cancellationToken) => ValidateAsync(dbContext, command, context, cancellationToken))
            .When(x => x.Lines.Count is > 0 and <= MaxLines);
    }

    private static async Task ValidateAsync(
        IApplicationDbContext dbContext,
        ConfirmGoodsReceiptImportCommand command,
        ValidationContext<ConfirmGoodsReceiptImportCommand> context,
        CancellationToken cancellationToken)
    {
        var lines = command.Lines;

        var itemIds = lines.Where(l => l.ItemId is not null).Select(l => l.ItemId!.Value).Distinct().ToList();
        var locationIds = lines.Select(l => l.LocationId).Distinct().ToList();

        var perishableByItemId = await dbContext.Items
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.IsPerishable, cancellationToken);

        var existingLocationIds = (await dbContext.Locations
                .AsNoTracking()
                .Where(l => locationIds.Contains(l.Id))
                .Select(l => l.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var prefix = $"Lines[{i}]";

            if (line.ItemId is { } itemId)
            {
                if (!perishableByItemId.TryGetValue(itemId, out var isPerishable))
                {
                    context.AddFailure(new ValidationFailure($"{prefix}.ItemId", "Item does not exist")
                    {
                        ErrorCode = ValidationErrorCodes.InvalidReference
                    });
                }
                else
                {
                    ValidateExpiry(context, prefix, isPerishable, line.ExpiryDate);
                }
            }
            else
            {
                ValidateExpiry(context, prefix, line.IsPerishable, line.ExpiryDate);
            }

            if (!existingLocationIds.Contains(line.LocationId))
            {
                context.AddFailure(new ValidationFailure($"{prefix}.LocationId", "Location does not exist")
                {
                    ErrorCode = ValidationErrorCodes.InvalidReference
                });
            }
        }

        await ValidateSplitQuantitiesAsync(dbContext, command, context, cancellationToken);
    }

    private static void ValidateExpiry(
        ValidationContext<ConfirmGoodsReceiptImportCommand> context, string prefix, bool isPerishable, DateOnly? expiryDate)
    {
        if (isPerishable && expiryDate is null)
        {
            context.AddFailure(new ValidationFailure($"{prefix}.ExpiryDate", "Expiry date is required for perishable items")
            {
                ErrorCode = ValidationErrorCodes.ExpiryDateRequired
            });
        }
        else if (!isPerishable && expiryDate is not null)
        {
            context.AddFailure(new ValidationFailure($"{prefix}.ExpiryDate", "Expiry date is not allowed for non-perishable items")
            {
                ErrorCode = ValidationErrorCodes.ExpiryDateNotAllowed
            });
        }
    }

    private static async Task ValidateSplitQuantitiesAsync(
        IApplicationDbContext dbContext,
        ConfirmGoodsReceiptImportCommand command,
        ValidationContext<ConfirmGoodsReceiptImportCommand> context,
        CancellationToken cancellationToken)
    {
        var extractedJson = await dbContext.GoodsReceiptImports
            .AsNoTracking()
            .Where(i => i.Id == command.ImportId)
            .Select(i => i.ExtractedDataJson)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(extractedJson))
            return;

        var extraction = JsonSerializer.Deserialize<GoodsReceiptExtractionResult>(extractedJson);
        if (extraction is null || extraction.LineItems.Count == 0)
            return;

        // Group the (possibly split) confirm lines back onto the source extraction line they came from,
        // and require each group's quantities to sum to the original extracted quantity. A source line
        // dropped entirely (no confirm lines) is allowed; a present group that doesn't add up is not.
        var quantityBySource = command.Lines
            .GroupBy(l => l.SourceLineIndex)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        foreach (var (sourceIndex, totalQuantity) in quantityBySource)
        {
            if (sourceIndex < 0 || sourceIndex >= extraction.LineItems.Count)
                continue;

            var originalQuantity = extraction.LineItems[sourceIndex].Quantity;
            if (totalQuantity != originalQuantity)
            {
                context.AddFailure(new ValidationFailure("Lines",
                    $"Split lines for '{extraction.LineItems[sourceIndex].Name}' sum to {totalQuantity} but the extracted quantity is {originalQuantity}.")
                {
                    ErrorCode = ValidationErrorCodes.SplitQuantityMismatch
                });
            }
        }
    }
}
