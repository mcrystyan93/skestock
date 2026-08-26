using skestock.Application.Common.Caching;
using skestock.Application.Features.Locations.Models;

namespace skestock.Application.Features.Locations.Commands.CreateLocation;

public class CreateLocationCommand : IRequest<Result<LocationDto>>, ICacheInvalidation
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int? ParentLocationId { get; init; }

    // Invalidate every cached GetAllLocations page/filter/sort combination - a new location
    // can affect any of them (default sort, search matches, filters, etc.).
    public IReadOnlyCollection<string> Tags => [CacheConstants.LocationListTag];
}
