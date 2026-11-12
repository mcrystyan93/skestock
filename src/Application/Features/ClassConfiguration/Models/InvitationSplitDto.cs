namespace skestock.Application.Features.ClassConfiguration.Models;

public sealed record InvitationSplitDto(
    int StudentInvitationCount,
    int InstructorInvitationCount,
    int OpeningPrayerInvitationCount,
    int ExperienceInvitationCount,
    int ChangeInvitationCount);
