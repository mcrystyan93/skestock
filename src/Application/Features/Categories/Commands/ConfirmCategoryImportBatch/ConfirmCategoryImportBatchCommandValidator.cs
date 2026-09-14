using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Categories.Commands.ConfirmCategoryImportBatch;

public class ConfirmCategoryImportBatchCommandValidator : AbstractValidator<ConfirmCategoryImportBatchCommand>
{
    private const int NameMaxLength = 100;
    private const int MaxCategories = 200;

    public ConfirmCategoryImportBatchCommandValidator()
    {
        RuleFor(command => command.BatchId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(command => command.CategoryNames)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode(ValidationErrorCodes.Required)
            .Must(names => names.Count <= MaxCategories)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage($"A category import batch cannot confirm more than {MaxCategories} categories");

        RuleForEach(command => command.CategoryNames).ChildRules(name =>
        {
            name.RuleFor(value => value)
                .Must(value => !string.IsNullOrWhiteSpace(value))
                .WithErrorCode(ValidationErrorCodes.Required)
                .WithMessage("Category names cannot be blank")
                .MaximumLength(NameMaxLength)
                .WithErrorCode(ValidationErrorCodes.MaxLength);
        });
    }
}
