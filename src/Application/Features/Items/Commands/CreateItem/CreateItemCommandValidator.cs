using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Items.Commands.CreateItem;

public class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    // Keep in sync with ItemConfiguration's HasMaxLength (Infrastructure.Data.Configurations.
    // DataSchemaConstants) - Application can't reference Infrastructure.
    private const int NameMaxLength = 100;
    private const int DescriptionMaxLength = 500;
    private const int SkuMaxLength = 50;
    private const int UnitMaxLength = 20;

    public CreateItemCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(NameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(DescriptionMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.Unit)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(UnitMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.MinThreshold)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);

        RuleFor(x => x.Sku)
            .MaximumLength(SkuMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .DependentRules(() =>
            {
                RuleFor(x => x.Sku)
                    .MustAsync((sku, cancellationToken) => IsSkuUniqueAsync(dbContext, sku!, cancellationToken))
                    .When(x => !string.IsNullOrWhiteSpace(x.Sku))
                    .WithMessage("An item with this SKU already exists")
                    .WithErrorCode(ValidationErrorCodes.DuplicateSku);
            });

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan)
            .DependentRules(() =>
            {
                RuleFor(x => x.CategoryId)
                    .MustAsync((categoryId, cancellationToken) => CategoryExistsAsync(dbContext, categoryId, cancellationToken))
                    .WithMessage("Category does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });
    }

    private static async Task<bool> IsSkuUniqueAsync(IApplicationDbContext dbContext, string sku, CancellationToken cancellationToken)
    {
        var normalized = sku.Trim().ToLower();
        return !await dbContext.Items
            .AsNoTracking()
            .AnyAsync(i => i.Sku != null && i.Sku.ToLower() == normalized, cancellationToken);
    }

    private static async Task<bool> CategoryExistsAsync(IApplicationDbContext dbContext, int categoryId, CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == categoryId, cancellationToken);
    }
}
