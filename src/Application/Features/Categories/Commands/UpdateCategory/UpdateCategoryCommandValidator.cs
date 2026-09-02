using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    // Keep in sync with CategoryConfiguration's HasMaxLength (Infrastructure.Data.Configurations.
    // DataSchemaConstants.DEFAULT_NAME_LENGTH) - Application can't reference Infrastructure.
    private const int NameMaxLength = 100;

    public UpdateCategoryCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(NameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .MustAsync((command, cancellationToken) => IsNameUniqueAsync(dbContext, command.Id, command.Name, cancellationToken))
                    .WithName(nameof(UpdateCategoryCommand.Name))
                    .WithMessage("A category with this name already exists")
                    .WithErrorCode(ValidationErrorCodes.DuplicateName);
            });
    }

    private static async Task<bool> IsNameUniqueAsync(IApplicationDbContext dbContext, Guid id, string name, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        return !await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id != id && c.Name.ToLower() == normalized, cancellationToken);
    }
}
