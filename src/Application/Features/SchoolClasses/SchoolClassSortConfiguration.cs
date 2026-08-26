using System.Linq.Expressions;
using skestock.Application.Common.Keyset;
using skestock.Domain.Entities;

namespace skestock.Application.Features.SchoolClasses;

public sealed class SchoolClassSortConfiguration : IKeysetSortConfiguration<SchoolClass>
{
    public IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = ["Name", "Id"],
            ["startDate"] = ["StartDate", "Id"],
            ["endDate"] = ["EndDate", "Id"],
            ["createdDate"] = ["Created", "Id"],
            ["lastModifiedDate"] = ["LastModified", "Id"],
            ["id"] = ["Id"]
        };

    public List<(string Key, string Direction)> DefaultSort { get; } = [("Created", "desc"), ("Id", "desc")];

    public Expression<Func<SchoolClass, dynamic>> GetPropertyExpression(string propertyName)
    {
        return propertyName switch
        {
            "Name" => d => d.Name,
            "StartDate" => d => d.StartDate,
            "EndDate" => d => d.EndDate,
            "Created" => d => d.CreatedDate,
            "LastModified" => d => d.LastModifiedDate,
            "Id" => d => d.Id,
            _ => d => d.Id
        };
    }

    public object? GetPropertyValue(SchoolClass entity, string propertyName)
    {
        return propertyName switch
        {
            "Name" => entity.Name,
            "StartDate" => entity.StartDate,
            "EndDate" => entity.EndDate,
            "Created" => entity.CreatedDate,
            "LastModified" => entity.LastModifiedDate,
            "Id" => entity.Id,
            _ => null
        };
    }
}
