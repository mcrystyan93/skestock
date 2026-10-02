using skestock.Application.Common.Errors;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationCount;

public sealed class SaveInvitationCountCommandValidator : AbstractValidator<SaveInvitationCountCommand>
{
    public SaveInvitationCountCommandValidator()
    {
        RuleFor(command => command.InvitationCount).GreaterThanOrEqualTo(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
    }
}
