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
        var query = dbContext.Categories.AsNoTracking().AsQueryable();
        query = TextSearchCollation.IsSqlServer(dbContext.Database)
            ? query.Where(c =>
                EF.Functions.Collate(c.Name.Trim(), TextSearchCollation.AccentInsensitive) == normalized)
            : query.Where(c => c.Name.Trim().ToLower() == normalized);
        return !await query.AnyAsync(cancellationToken);
    }
}
