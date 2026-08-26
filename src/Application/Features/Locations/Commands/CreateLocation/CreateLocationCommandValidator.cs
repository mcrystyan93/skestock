using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Locations.Commands.CreateLocation;

public class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommand>
{
    // Keep in sync with LocationConfiguration's HasMaxLength (Infrastructure.Data.Configurations.
    // DataSchemaConstants.DEFAULT_NAME_LENGTH) - Application can't reference Infrastructure.
    private const int NameMaxLength = 100;
    private const int TypeMaxLength = 50;

    public CreateLocationCommandValidator(IApplicationDbContext dbContext)
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
                    .WithMessage("A location with this name already exists")
                    .WithErrorCode(ValidationErrorCodes.DuplicateName);
            });

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(TypeMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.ParentLocationId)
            .MustAsync((parentLocationId, cancellationToken) => ParentLocationExistsAsync(dbContext, parentLocationId, cancellationToken))
            .When(x => x.ParentLocationId.HasValue)
            .WithMessage("Parent location does not exist")
            .WithErrorCode(ValidationErrorCodes.InvalidReference);
    }

    private static async Task<bool> IsNameUniqueAsync(IApplicationDbContext dbContext, string name, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        return !await dbContext.Locations
            .AsNoTracking()
            .AnyAsync(l => l.Name.ToLower() == normalized, cancellationToken);
    }

    private static async Task<bool> ParentLocationExistsAsync(IApplicationDbContext dbContext, int? parentLocationId, CancellationToken cancellationToken)
    {
        return await dbContext.Locations
            .AsNoTracking()
            .AnyAsync(l => l.Id == parentLocationId, cancellationToken);
    }
}
