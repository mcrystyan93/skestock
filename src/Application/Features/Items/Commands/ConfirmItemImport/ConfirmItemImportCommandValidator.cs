using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Items.Commands.ConfirmItemImport;

public class ConfirmItemImportCommandValidator : AbstractValidator<ConfirmItemImportCommand>
{
    // Keep in sync with ItemConfiguration/CategoryConfiguration's HasMaxLength (Infrastructure.Data.
    // Configurations.DataSchemaConstants) - Application can't reference Infrastructure.
    private const int NameMaxLength = 100;
    private const int SkuMaxLength = 50;
    private const int UnitMaxLength = 20;
    private const int DescriptionMaxLength = 500;
    private const int MaxItems = 500;

    public ConfirmItemImportCommandValidator()
    {
        RuleFor(x => x.ImportId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.Items)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode(ValidationErrorCodes.Required)
            .Must(items => items.Count <= MaxItems)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage($"An item import cannot confirm more than {MaxItems} items");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ItemId)
                .NotEmpty()
                .WithErrorCode(ValidationErrorCodes.Required)
                .WithMessage("An item is required");

            item.RuleFor(i => i.Name)
                .Must(n => !string.IsNullOrWhiteSpace(n))
                .WithErrorCode(ValidationErrorCodes.Required)
                .WithMessage("Item names cannot be blank")
                .MaximumLength(NameMaxLength)
                .WithErrorCode(ValidationErrorCodes.MaxLength);

            item.RuleFor(i => i.Sku)
                .MaximumLength(SkuMaxLength)
                .WithErrorCode(ValidationErrorCodes.MaxLength);

            item.RuleFor(i => i.Unit)
                .MaximumLength(UnitMaxLength)
                .WithErrorCode(ValidationErrorCodes.MaxLength);

            item.RuleFor(i => i.Description)
                .MaximumLength(DescriptionMaxLength)
                .WithErrorCode(ValidationErrorCodes.MaxLength);

        });
    }
}
