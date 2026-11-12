using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Constants;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationSplit;

[Authorize(Roles = Roles.Administrator)]
public sealed class SaveInvitationSplitCommand : IRequest<Result<InvitationSplitDto>>, ICacheInvalidation
{
    public int? StudentInvitationCount { get; init; }
    public int? InstructorInvitationCount { get; init; }
    public int? OpeningPrayerInvitationCount { get; init; }
    public int? ExperienceInvitationCount { get; init; }
    public int? ChangeInvitationCount { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.ConfigurationTag, CacheConstants.InvitationSplitTag];
}
