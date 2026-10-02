using skestock.Application.Common.Errors;

namespace skestock.Application.Features.ClassConfiguration.Commands.DeleteDepartment;

public sealed class DeleteDepartmentCommandValidator : AbstractValidator<DeleteDepartmentCommand>
{
    public DeleteDepartmentCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
    }
}
