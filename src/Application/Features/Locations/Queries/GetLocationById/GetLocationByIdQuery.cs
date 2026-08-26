using skestock.Application.Features.Locations.Models;

namespace skestock.Application.Features.Locations.Queries.GetLocationById;

public class GetLocationByIdQuery : IRequest<Result<LocationDto>>
{
    public int Id { get; init; }
}
