using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.Locations.Models;

public static class LocationRequests
{
    public class GetAllLocationsRequest: BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public class CreateLocationRequest
    {
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public Guid? ParentLocationId { get; init; }
    }

    public class UpdateLocationRequest
    {
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public Guid? ParentLocationId { get; init; }
    }
}
