using skestock.Application.Features.Locations.Models;

namespace skestock.Application.Features.Locations.Queries.GetLocationById;

public class GetLocationByIdQuery : IRequest<Result<LocationDto>>
{
    public Guid Id { get; init; }
}
