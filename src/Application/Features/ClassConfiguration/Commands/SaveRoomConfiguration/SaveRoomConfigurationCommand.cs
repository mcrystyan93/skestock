using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Constants;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveRoomConfiguration;

[Authorize(Roles = Roles.Administrator)]
public sealed class SaveRoomConfigurationCommand : IRequest<Result<RoomConfigurationDto>>, ICacheInvalidation
{
    public int? Room4SeatCount { get; init; }
    public int? Room2SeatCount { get; init; }
    public int? Room6SeatCount { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.ConfigurationTag, CacheConstants.RoomConfigurationTag];
}
