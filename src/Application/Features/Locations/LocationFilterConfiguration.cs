using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Locations;

public class LocationFilterConfiguration: IFilterConfiguration<Location>
{
    public IReadOnlyDictionary<string, FilterField<Location>> Fields { get; } = new Dictionary<string, FilterField<Location>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<Location, Guid>>)(d => d.Id), typeof(Guid)),
        ["name"] = new((Expression<Func<Location, string>>)(d => d.Name), typeof(string)),
        ["type"] = new((Expression<Func<Location, string>>)(d => d.Type), typeof(string)),
        ["parentLocationId"] = new((Expression<Func<Location, Guid?>>)(d => d.ParentLocationId), typeof(Guid?)),
        ["createdDate"] = new((Expression<Func<Location, DateTimeOffset>>)(d => d.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<Location, DateTimeOffset>>)(d => d.LastModifiedDate), typeof(DateTimeOffset))
    };
}
