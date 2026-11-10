using skestock.Application.Common.Errors;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationSplit;

public sealed class SaveInvitationSplitCommandValidator : AbstractValidator<SaveInvitationSplitCommand>
{
    private const int MaximumInvitationCount = 200;

    public SaveInvitationSplitCommandValidator()
    {
        RuleFor(command => command.StudentInvitationCount).NotNull()
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(command => command.StudentInvitationCount).GreaterThanOrEqualTo(0)
            .When(command => command.StudentInvitationCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
        RuleFor(command => command.StudentInvitationCount).LessThanOrEqualTo(MaximumInvitationCount)
            .When(command => command.StudentInvitationCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.LessThanOrEqualTo);
        RuleFor(command => command.InstructorInvitationCount).NotNull()
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(command => command.InstructorInvitationCount).GreaterThanOrEqualTo(0)
            .When(command => command.InstructorInvitationCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
        RuleFor(command => command.InstructorInvitationCount).LessThanOrEqualTo(MaximumInvitationCount)
            .When(command => command.InstructorInvitationCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.LessThanOrEqualTo);
        RuleFor(command => command.OpeningPrayerInvitationCount).NotNull()
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(command => command.OpeningPrayerInvitationCount).GreaterThanOrEqualTo(0)
            .When(command => command.OpeningPrayerInvitationCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
        RuleFor(command => command.OpeningPrayerInvitationCount).LessThanOrEqualTo(MaximumInvitationCount)
            .When(command => command.OpeningPrayerInvitationCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.LessThanOrEqualTo);
        RuleFor(command => command.ExperienceInvitationCount).NotNull()
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(command => command.ExperienceInvitationCount).GreaterThanOrEqualTo(0)
            .When(command => command.ExperienceInvitationCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
        RuleFor(command => command.ExperienceInvitationCount).LessThanOrEqualTo(MaximumInvitationCount)
            .When(command => command.ExperienceInvitationCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.LessThanOrEqualTo);
        RuleFor(command => command.ChangeInvitationCount).NotNull()
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(command => command.ChangeInvitationCount).GreaterThanOrEqualTo(0)
            .When(command => command.ChangeInvitationCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
        RuleFor(command => command.ChangeInvitationCount).LessThanOrEqualTo(MaximumInvitationCount)
            .When(command => command.ChangeInvitationCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.LessThanOrEqualTo);
    }
}
