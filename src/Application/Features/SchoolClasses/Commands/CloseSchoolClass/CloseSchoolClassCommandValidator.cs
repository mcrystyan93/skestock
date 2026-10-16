using skestock.Application.Common.Errors;

namespace skestock.Application.Features.SchoolClasses.Commands.CloseSchoolClass;

public sealed class CloseSchoolClassCommandValidator : AbstractValidator<CloseSchoolClassCommand>
{
    public CloseSchoolClassCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.TargetClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.TargetClassId)
            .NotEqual(x => x.Id)
            .WithMessage("The target class must be different from the class being closed")
            .WithErrorCode(ValidationErrorCodes.SelfReference);
    }
}
