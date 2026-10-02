using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveRoomConfiguration;

public sealed class SaveRoomConfigurationCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SaveRoomConfigurationCommand, Result<RoomConfigurationDto>>
{
    public async ValueTask<Result<RoomConfigurationDto>> Handle(SaveRoomConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        var configuration = await dbContext.SharedClassConfigurations
            .SingleOrDefaultAsync(item => item.Id == SharedClassConfiguration.SingletonId, cancellationToken);
        if (configuration is null)
        {
            configuration = new SharedClassConfiguration();
            dbContext.SharedClassConfigurations.Add(configuration);
        }

        configuration.Room4SeatCount = request.Room4SeatCount!.Value;
        configuration.Room1SeatCount = request.Room1SeatCount!.Value;
        configuration.Room6SeatCount = request.Room6SeatCount!.Value;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok(new RoomConfigurationDto(
            configuration.Room4SeatCount, configuration.Room1SeatCount, configuration.Room6SeatCount));
    }
}
