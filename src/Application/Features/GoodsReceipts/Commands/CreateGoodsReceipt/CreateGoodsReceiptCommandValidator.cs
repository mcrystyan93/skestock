using FluentValidation.Results;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceipt;

public class CreateGoodsReceiptCommandValidator : AbstractValidator<CreateGoodsReceiptCommand>
{
    // Keep in sync with GoodsReceiptConfiguration's HasMaxLength.
    private const int NoteMaxLength = 500;
    private const int SupplierReferenceMaxLength = 200;
    private const int MaxLines = 200;

    public CreateGoodsReceiptCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ClassId)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan)
            .DependentRules(() =>
            {
                RuleFor(x => x.ClassId)
                    .MustAsync((classId, cancellationToken) => SchoolClassExistsAsync(dbContext, classId, cancellationToken))
                    .WithMessage("School class does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });

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
            .WithMessage("A goods receipt must have at least one line")
            .Must(lines => lines.Count <= MaxLines)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage($"A goods receipt cannot have more than {MaxLines} lines");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId)
                .GreaterThan(0)
                .WithErrorCode(ValidationErrorCodes.GreaterThan);

            line.RuleFor(l => l.LocationId)
                .GreaterThan(0)
                .WithErrorCode(ValidationErrorCodes.GreaterThan);

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0)
                .WithErrorCode(ValidationErrorCodes.GreaterThan);
        });

        // Bulk (single round-trip) checks across all lines: item/location existence, the
        // expiry-date-vs-IsPerishable rule, and duplicate (ItemId, LocationId, ExpiryDate) lines
        // within the same request - catches a typo'd/duplicate scan server-side, backing up the
        // client's own review screen (see step 3 of the goods-receipt flow).
        RuleFor(x => x.Lines)
            .CustomAsync(async (lines, context, cancellationToken) =>
                await ValidateLinesAsync(dbContext, lines, context, cancellationToken))
            .When(x => x.Lines.Count > 0 && x.Lines.Count <= MaxLines);
    }

    private static async Task ValidateLinesAsync(
        IApplicationDbContext dbContext,
        List<CreateGoodsReceiptLine> lines,
        ValidationContext<CreateGoodsReceiptCommand> context,
        CancellationToken cancellationToken)
    {
        var itemIds = lines.Select(l => l.ItemId).Distinct().ToList();
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

        var seenLineKeys = new HashSet<(int ItemId, int LocationId, DateOnly? ExpiryDate)>();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var propertyPrefix = $"Lines[{i}]";

            if (!perishableByItemId.TryGetValue(line.ItemId, out var isPerishable))
            {
                context.AddFailure(new ValidationFailure($"{propertyPrefix}.ItemId", "Item does not exist")
                {
                    ErrorCode = ValidationErrorCodes.InvalidReference
                });
            }
            else if (isPerishable && line.ExpiryDate is null)
            {
                context.AddFailure(new ValidationFailure($"{propertyPrefix}.ExpiryDate", "Expiry date is required for perishable items")
                {
                    ErrorCode = ValidationErrorCodes.ExpiryDateRequired
                });
            }
            else if (!isPerishable && line.ExpiryDate is not null)
            {
                context.AddFailure(new ValidationFailure($"{propertyPrefix}.ExpiryDate", "Expiry date is not allowed for non-perishable items")
                {
                    ErrorCode = ValidationErrorCodes.ExpiryDateNotAllowed
                });
            }

            if (!existingLocationIds.Contains(line.LocationId))
            {
                context.AddFailure(new ValidationFailure($"{propertyPrefix}.LocationId", "Location does not exist")
                {
                    ErrorCode = ValidationErrorCodes.InvalidReference
                });
            }

            var lineKey = (line.ItemId, line.LocationId, line.ExpiryDate);
            if (!seenLineKeys.Add(lineKey))
            {
                context.AddFailure(new ValidationFailure(propertyPrefix, "Duplicate line: the same item, location, and expiry date already appear in this receipt")
                {
                    ErrorCode = ValidationErrorCodes.DuplicateReceiptLine
                });
            }
        }
    }

    private static async Task<bool> SchoolClassExistsAsync(IApplicationDbContext dbContext, int classId, CancellationToken cancellationToken)
    {
        return await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id == classId, cancellationToken);
    }
}
