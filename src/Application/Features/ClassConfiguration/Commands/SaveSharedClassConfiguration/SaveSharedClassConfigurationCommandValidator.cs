namespace skestock.Application.Features.ClassConfiguration.Commands.SaveSharedClassConfiguration;

public sealed class
    SaveSharedClassConfigurationCommandValidator : AbstractValidator<SaveSharedClassConfigurationCommand>
{
    private const int DepartmentNameMaxLength = 100;

    public SaveSharedClassConfigurationCommandValidator()
    {
        RuleFor(command => command.InvitationCount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Departments)
            .Must(departments => departments is not null &&
                                 departments.Select(department => department.Id).Where(id => id.HasValue).Distinct()
                                     .Count() ==
                                 departments.Count(department => department.Id.HasValue))
            .WithMessage("Un departament nu poate apărea de mai multe ori.");
        RuleFor(command => command.Departments)
            .Must(departments => departments is not null && departments
                .Select(department => department.Name?.Trim() ?? string.Empty)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() == departments.Count)
            .WithMessage("Numele departamentelor trebuie să fie unice.");
        RuleForEach(command => command.Departments).ChildRules(department =>
        {
            department.RuleFor(item => item.Name)
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Numele departamentului este obligatoriu.")
                .MaximumLength(DepartmentNameMaxLength);
            department.RuleFor(item => item.Responsibilities)
                .Must(responsibilities => !string.IsNullOrWhiteSpace(responsibilities))
                .WithMessage("Responsabilitățile sunt obligatorii.");
        });
    }
}
