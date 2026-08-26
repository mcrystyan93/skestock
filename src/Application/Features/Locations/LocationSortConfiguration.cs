using System.Linq.Expressions;
using skestock.Application.Common.Keyset;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Locations;

public sealed class LocationSortConfiguration : IKeysetSortConfiguration<Location>
{
    public IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = ["Name", "Id"],
            ["type"] = ["Type", "Id"],
            ["createdDate"] = ["Created", "Id"],
            ["lastModifiedDate"] = ["LastModified", "Id"],
            ["id"] = ["Id"]
        };

    public List<(string Key, string Direction)> DefaultSort { get; } = [("Created", "desc"), ("Id", "desc")];

    public Expression<Func<Location, dynamic>> GetPropertyExpression(string propertyName)
    {
        return propertyName switch
        {
            "Name" => d => d.Name,
            "Type" => d => d.Type,
            "Created" => d => d.CreatedDate,
            "LastModified" => d => d.LastModifiedDate,
            "Id" => d => d.Id,
            _ => d => d.Id
        };
    }

    public object? GetPropertyValue(Location entity, string propertyName)
    {
        return propertyName switch
        {
            "Name" => entity.Name,
            "Type" => entity.Type,
            "Created" => entity.CreatedDate,
            "LastModified" => entity.LastModifiedDate,
            "Id" => entity.Id,
            _ => null
        };
    }
}
