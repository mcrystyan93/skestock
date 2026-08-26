using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Locations.Commands.UpdateLocation;

public class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    // Keep in sync with LocationConfiguration's HasMaxLength (Infrastructure.Data.Configurations.
    // DataSchemaConstants.DEFAULT_NAME_LENGTH) - Application can't reference Infrastructure.
    private const int NameMaxLength = 100;
    private const int TypeMaxLength = 50;

    // Bounds the ancestor-chain walk in HasCircularReferenceAsync so a corrupted/very deep
    // hierarchy can't turn a single validation call into an unbounded loop.
    private const int MaxHierarchyDepth = 100;

    public UpdateLocationCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThan);

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(NameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .MustAsync((command, cancellationToken) => IsNameUniqueAsync(dbContext, command.Id, command.Name, cancellationToken))
                    .WithName(nameof(UpdateLocationCommand.Name))
                    .WithMessage("A location with this name already exists")
                    .WithErrorCode(ValidationErrorCodes.DuplicateName);
            });

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(TypeMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.ParentLocationId)
            .Must((command, parentLocationId) => parentLocationId != command.Id)
            .When(x => x.ParentLocationId.HasValue)
            .WithMessage("A location cannot be its own parent")
            .WithErrorCode(ValidationErrorCodes.SelfReference);

        RuleFor(x => x.ParentLocationId)
            .MustAsync((parentLocationId, cancellationToken) => ParentLocationExistsAsync(dbContext, parentLocationId, cancellationToken))
            .When(x => x.ParentLocationId.HasValue)
            .WithMessage("Parent location does not exist")
            .WithErrorCode(ValidationErrorCodes.InvalidReference)
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .MustAsync(async (command, cancellationToken) => !await HasCircularReferenceAsync(dbContext, command.Id, command.ParentLocationId, cancellationToken))
                    .When(x => x.ParentLocationId.HasValue && x.ParentLocationId != x.Id)
                    .WithName(nameof(UpdateLocationCommand.ParentLocationId))
                    .WithMessage("Assigning this parent would create a circular location hierarchy")
                    .WithErrorCode(ValidationErrorCodes.CircularReference);
            });
    }

    private static async Task<bool> IsNameUniqueAsync(IApplicationDbContext dbContext, int id, string name, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        return !await dbContext.Locations
            .AsNoTracking()
            .AnyAsync(l => l.Id != id && l.Name.ToLower() == normalized, cancellationToken);
    }

    private static async Task<bool> ParentLocationExistsAsync(IApplicationDbContext dbContext, int? parentLocationId, CancellationToken cancellationToken)
    {
        return await dbContext.Locations
            .AsNoTracking()
            .AnyAsync(l => l.Id == parentLocationId, cancellationToken);
    }

    /// <summary>
    /// Walks the ancestor chain starting at <paramref name="parentLocationId"/> to make sure
    /// <paramref name="id"/> does not appear in it - assigning such a parent would create a cycle
    /// in the self-referencing Location hierarchy.
    /// </summary>
    private static async Task<bool> HasCircularReferenceAsync(IApplicationDbContext dbContext, int id, int? parentLocationId, CancellationToken cancellationToken)
    {
        var currentId = parentLocationId;
        var depth = 0;

        while (currentId.HasValue)
        {
            if (currentId.Value == id)
                return true;

            if (++depth > MaxHierarchyDepth)
                return true; // Treat runaway/corrupted chains as circular rather than looping forever.

            currentId = await dbContext.Locations
                .AsNoTracking()
                .Where(l => l.Id == currentId.Value)
                .Select(l => (int?)l.ParentLocationId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return false;
    }
}
