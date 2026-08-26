using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.SchoolClasses.Commands.CreateSchoolClass;

public class CreateSchoolClassCommandValidator : AbstractValidator<CreateSchoolClassCommand>
{
    // Keep in sync with SchoolClassConfiguration's HasMaxLength (Infrastructure.Data.
    // Configurations.DataSchemaConstants.DEFAULT_NAME_LENGTH) - Application can't reference
    // Infrastructure.
    private const int NameMaxLength = 100;

    public CreateSchoolClassCommandValidator(IApplicationDbContext dbContext)
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
                    .WithMessage("A school class with this name already exists")
                    .WithErrorCode(ValidationErrorCodes.DuplicateName);
            });

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithErrorCode(ValidationErrorCodes.InvalidEnum);

        RuleFor(x => x)
            .Must(x => x.StartDate < x.EndDate)
            .WithName(nameof(CreateSchoolClassCommand.EndDate))
            .WithMessage("EndDate must be after StartDate")
            .WithErrorCode(ValidationErrorCodes.InvalidDateRange);
    }

    private static async Task<bool> IsNameUniqueAsync(IApplicationDbContext dbContext, string name, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        return !await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Name.ToLower() == normalized, cancellationToken);
    }
}
