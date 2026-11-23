using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    // Keep in sync with CategoryConfiguration's HasMaxLength (Infrastructure.Data.Configurations.
    // DataSchemaConstants.DEFAULT_NAME_LENGTH) - Application can't reference Infrastructure.
    private const int NameMaxLength = 100;

    // Bounds the ancestor-chain walk in HasCircularReferenceAsync so a corrupted/very deep
    // hierarchy can't turn a single validation call into an unbounded loop.
    private const int MaxHierarchyDepth = 100;

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

        RuleFor(x => x.ParentId)
            .Must((command, parentId) => parentId != command.Id)
            .When(x => x.ParentId.HasValue)
            .WithMessage("A category cannot be its own parent")
            .WithErrorCode(ValidationErrorCodes.SelfReference);

        RuleFor(x => x.ParentId)
            .MustAsync(async (parentId, cancellationToken) =>
                !await dbContext.Items.AsNoTracking().AnyAsync(i => i.CategoryId == parentId, cancellationToken))
            .When(x => x.ParentId.HasValue)
            .WithMessage("A category that has items cannot be a parent")
            .WithErrorCode(ValidationErrorCodes.ParentCategoryHasItems);

        RuleFor(x => x.ParentId)
            .MustAsync((parentId, cancellationToken) => ParentExistsAsync(dbContext, parentId, cancellationToken))
            .When(x => x.ParentId.HasValue)
            .WithMessage("Parent category does not exist")
            .WithErrorCode(ValidationErrorCodes.InvalidReference)
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .MustAsync(async (command, cancellationToken) => !await HasCircularReferenceAsync(dbContext, command.Id, command.ParentId, cancellationToken))
                    .When(x => x.ParentId.HasValue && x.ParentId != x.Id)
                    .WithName(nameof(UpdateCategoryCommand.ParentId))
                    .WithMessage("Assigning this parent would create a circular category hierarchy")
                    .WithErrorCode(ValidationErrorCodes.CircularReference);
            });
    }

    private static Task<bool> ParentExistsAsync(IApplicationDbContext dbContext, Guid? parentId, CancellationToken cancellationToken) =>
        dbContext.Categories.AsNoTracking().AnyAsync(c => c.Id == parentId, cancellationToken);

    /// <summary>
    /// Walks the ancestor chain starting at <paramref name="parentId"/> to make sure <paramref name="id"/>
    /// does not appear in it - assigning such a parent would create a cycle.
    /// </summary>
    private static async Task<bool> HasCircularReferenceAsync(IApplicationDbContext dbContext, Guid id, Guid? parentId, CancellationToken cancellationToken)
    {
        var currentId = parentId;
        var depth = 0;

        while (currentId.HasValue)
        {
            if (currentId.Value == id)
                return true;

            if (++depth > MaxHierarchyDepth)
                return true; // Treat runaway/corrupted chains as circular rather than looping forever.

            currentId = await dbContext.Categories
                .AsNoTracking()
                .Where(c => c.Id == currentId.Value)
                .Select(c => c.ParentId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return false;
    }

    private static async Task<bool> IsNameUniqueAsync(IApplicationDbContext dbContext, Guid id, string name, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        var query = dbContext.Categories.AsNoTracking().AsQueryable();
        query = TextSearchCollation.IsSqlServer(dbContext.Database)
            ? query.Where(c => c.Id != id &&
                EF.Functions.Collate(c.Name.Trim(), TextSearchCollation.AccentInsensitive) == normalized)
            : query.Where(c => c.Id != id && c.Name.Trim().ToLower() == normalized);
        return !await query.AnyAsync(cancellationToken);
    }
}
