using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Constants;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationCount;

[Authorize(Roles = Roles.Administrator)]
public sealed class SaveInvitationCountCommand : IRequest<Result<InvitationCountDto>>, ICacheInvalidation
{
    public int InvitationCount { get; init; }
    public IReadOnlyCollection<string> Tags => [CacheConstants.ConfigurationTag, CacheConstants.InvitationCountTag];
}
