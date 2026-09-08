using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Categories.Commands.ConfirmCategoryImport;

public class ConfirmCategoryImportCommandValidator : AbstractValidator<ConfirmCategoryImportCommand>
{
    // Keep in sync with CategoryConfiguration's HasMaxLength (Infrastructure.Data.Configurations.
    // DataSchemaConstants.DEFAULT_NAME_LENGTH) - Application can't reference Infrastructure.
    private const int NameMaxLength = 100;
    private const int MaxCategories = 200;

    public ConfirmCategoryImportCommandValidator()
    {
        RuleFor(x => x.ImportId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.CategoryNames)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode(ValidationErrorCodes.Required)
            .Must(names => names.Count <= MaxCategories)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage($"A category import cannot confirm more than {MaxCategories} categories");

        RuleForEach(x => x.CategoryNames).ChildRules(name =>
        {
            name.RuleFor(n => n)
                .Must(n => !string.IsNullOrWhiteSpace(n))
                .WithErrorCode(ValidationErrorCodes.Required)
                .WithMessage("Category names cannot be blank")
                .MaximumLength(NameMaxLength)
                .WithErrorCode(ValidationErrorCodes.MaxLength);
        });
    }
}
