using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Congregations.Commands.CreateCongregation;

public class CreateCongregationCommandValidator : AbstractValidator<CreateCongregationCommand>
{
    // Keep in sync with CongregationConfiguration's HasMaxLength - Application can't reference Infrastructure.
    private const int NameMaxLength = 100;
    private const int EmailMaxLength = 200;
    private const int PersonMaxLength = 100;
    private const int PhoneMaxLength = 30;
    private const int MessageMaxLength = 1000;

    public CreateCongregationCommandValidator(IApplicationDbContext dbContext)
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
                    .WithMessage("A congregation with this name already exists")
                    .WithErrorCode(ValidationErrorCodes.DuplicateName);
            });

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(EmailMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .EmailAddress()
            .WithErrorCode(ValidationErrorCodes.Email);

        RuleFor(x => x.Coordinator).MaximumLength(PersonMaxLength).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.DessertContactPerson).MaximumLength(PersonMaxLength).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.CoordinatorPhone).MaximumLength(PhoneMaxLength).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.DessertContactPhone).MaximumLength(PhoneMaxLength).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.Message).MaximumLength(MessageMaxLength).WithErrorCode(ValidationErrorCodes.MaxLength);
    }

    // Soft-deleted rows are excluded by the entity's global query filter.
    private static async Task<bool> IsNameUniqueAsync(IApplicationDbContext dbContext, string name, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        var query = dbContext.Congregations.AsNoTracking().AsQueryable();
        query = TextSearchCollation.IsSqlServer(dbContext.Database)
            ? query.Where(c =>
                EF.Functions.Collate(c.Name.Trim(), TextSearchCollation.AccentInsensitive) == normalized)
            : query.Where(c => c.Name.Trim().ToLower() == normalized);
        return !await query.AnyAsync(cancellationToken);
    }
}
