using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetRoomConfiguration;

public sealed class GetRoomConfigurationQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetRoomConfigurationQuery, Result<RoomConfigurationDto>>
{
    public async ValueTask<Result<RoomConfigurationDto>> Handle(GetRoomConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        var configuration = await dbContext.SharedClassConfigurations.AsNoTracking()
            .Where(item => item.Id == SharedClassConfiguration.SingletonId)
            .Select(item => new RoomConfigurationDto(item.Room4SeatCount, item.Room2SeatCount, item.Room6SeatCount))
            .SingleOrDefaultAsync(cancellationToken);

        return Result.Ok(configuration ?? new RoomConfigurationDto(0, 0, 0));
    }
}
