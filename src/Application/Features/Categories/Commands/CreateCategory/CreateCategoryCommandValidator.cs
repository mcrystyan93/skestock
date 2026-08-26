using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    // Keep in sync with CategoryConfiguration's HasMaxLength (Infrastructure.Data.Configurations.
    // DataSchemaConstants.DEFAULT_NAME_LENGTH) - Application can't reference Infrastructure.
    private const int NameMaxLength = 100;

    public CreateCategoryCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(NameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .DependentRules(() =>
            {
                RuleFor(x => x.Name)
                    .MustAsync((name, cancellationToken) => IsNameUniqueAsync(dbContext, name, cancellationToken))
                    .WithMessage("A category with this name already exists")
                    .WithErrorCode(ValidationErrorCodes.DuplicateName);
            });
    }

    private static async Task<bool> IsNameUniqueAsync(IApplicationDbContext dbContext, string name, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        return !await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Name.ToLower() == normalized, cancellationToken);
    }
}
