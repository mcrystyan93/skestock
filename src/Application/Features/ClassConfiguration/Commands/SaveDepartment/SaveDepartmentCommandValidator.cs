using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveDepartment;

public sealed class SaveDepartmentCommandValidator : AbstractValidator<SaveDepartmentCommand>
{
    private const int DepartmentNameMaxLength = 100;

    public SaveDepartmentCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(command => command.Id).Must(id => id is null || id != Guid.Empty)
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(command => command.Name).Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(DepartmentNameMaxLength).WithErrorCode(ValidationErrorCodes.MaxLength)
            .DependentRules(() =>
            {
                RuleFor(command => command).MustAsync(async (command, token) =>
                {
                    var normalizedName = command.Name.Trim().ToLower();
                    return !await dbContext.SharedClassConfigurations
                        .Where(configuration => configuration.Id == SharedClassConfiguration.SingletonId)
                        .SelectMany(configuration => configuration.DepartmentTemplates)
                        .AnyAsync(department => department.Id != command.Id && department.Name.Trim().ToLower() == normalizedName, token);
                }).WithName(nameof(SaveDepartmentCommand.Name))
                    .WithMessage("A department with this name already exists.")
                    .WithErrorCode(ValidationErrorCodes.DuplicateName);
            });
        RuleFor(command => command.Responsibilities)
            .Must(responsibilities => !string.IsNullOrWhiteSpace(responsibilities))
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
