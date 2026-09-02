using skestock.Application.Common.Caching;
using skestock.Application.Features.Locations.Models;

namespace skestock.Application.Features.Locations.Commands.UpdateLocation;

public class UpdateLocationCommand : IRequest<Result<LocationDto>>, ICacheInvalidation
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public Guid? ParentLocationId { get; init; }

    // Invalidate every cached GetAllLocations page/filter/sort combination - an updated location
    // can affect any of them (default sort, search matches, filters, etc.).
    public IReadOnlyCollection<string> Tags => [CacheConstants.LocationListTag];
}
