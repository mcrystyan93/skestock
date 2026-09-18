using System.Linq.Expressions;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Features.OrderLists;

internal static class OrderListLineValidationExtensions
{
    private const int ProductNameMaxLength = 500;
    private const int UnitMaxLength = 50;
    private const int NotesMaxLength = 1000;

    // Shared per-line rules for create/update: quantity, product name, unit/notes lengths, and
    // that a referenced ItemId points at an existing, active catalog item.
    public static void AddOrderListLineRules<TCommand>(
        this AbstractValidator<TCommand> validator,
        Expression<Func<TCommand, IEnumerable<OrderListLineInput>>> linesSelector,
        IApplicationDbContext dbContext)
    {
        validator.RuleForEach(linesSelector)
            .ChildRules(line =>
            {
                line.RuleFor(l => l.Quantity)
                    .GreaterThan(0)
                    .WithErrorCode(ValidationErrorCodes.GreaterThan);

                line.RuleFor(l => l.ProductName)
                    .NotEmpty()
                    .When(l => !l.ItemId.HasValue)
                    .WithMessage("A product name is required when no catalog item is selected")
                    .WithErrorCode(ValidationErrorCodes.Required);

                line.RuleFor(l => l.ProductName)
                    .MaximumLength(ProductNameMaxLength)
                    .WithErrorCode(ValidationErrorCodes.MaxLength);

                line.RuleFor(l => l.Unit)
                    .MaximumLength(UnitMaxLength)
                    .WithErrorCode(ValidationErrorCodes.MaxLength);

                line.RuleFor(l => l.Notes)
                    .MaximumLength(NotesMaxLength)
                    .WithErrorCode(ValidationErrorCodes.MaxLength);

                line.RuleFor(l => l.ItemId)
                    .MustAsync((itemId, ct) => ItemExistsAndActiveAsync(dbContext, itemId!.Value, ct))
                    .When(l => l.ItemId.HasValue)
                    .WithMessage("The referenced item does not exist or is not active")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });
    }

    private static async Task<bool> ItemExistsAndActiveAsync(IApplicationDbContext dbContext, Guid itemId, CancellationToken cancellationToken)
    {
        return await dbContext.Items
            .AsNoTracking()
            .AnyAsync(i => i.Id == itemId && i.IsActive, cancellationToken);
    }
}
