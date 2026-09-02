using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.SchoolClasses.Commands.UpdateSchoolClass;

public class UpdateSchoolClassCommandValidator : AbstractValidator<UpdateSchoolClassCommand>
{
    // Keep in sync with SchoolClassConfiguration's HasMaxLength (Infrastructure.Data.
    // Configurations.DataSchemaConstants.DEFAULT_NAME_LENGTH) - Application can't reference
    // Infrastructure.
    private const int NameMaxLength = 100;

    public UpdateSchoolClassCommandValidator(IApplicationDbContext dbContext)
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
                    .WithName(nameof(UpdateSchoolClassCommand.Name))
                    .WithMessage("A school class with this name already exists")
                    .WithErrorCode(ValidationErrorCodes.DuplicateName);
            });

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithErrorCode(ValidationErrorCodes.InvalidEnum);

        RuleFor(x => x)
            .Must(x => x.StartDate < x.EndDate)
            .WithName(nameof(UpdateSchoolClassCommand.EndDate))
            .WithMessage("EndDate must be after StartDate")
            .WithErrorCode(ValidationErrorCodes.InvalidDateRange);
    }

    private static async Task<bool> IsNameUniqueAsync(IApplicationDbContext dbContext, Guid id, string name, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        return !await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id != id && c.Name.ToLower() == normalized, cancellationToken);
    }
}
