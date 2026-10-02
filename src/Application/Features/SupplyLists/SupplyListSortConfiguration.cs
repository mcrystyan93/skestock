using System.Linq.Expressions;
using skestock.Application.Common.Keyset;
using skestock.Domain.Entities;
using skestock.Domain.Entities.SupplyLists;

namespace skestock.Application.Features.SupplyLists;

public sealed class SupplyListSortConfiguration : IKeysetSortConfiguration<SupplyList>
{
    public IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["createdDate"] = ["CreatedDate", "Id"],
            ["lastModifiedDate"] = ["LastModifiedDate", "Id"],
            ["name"] = ["Name", "Id"],
            ["frequency"] = ["Frequency", "Id"],
            ["id"] = ["Id"]
        };

    public List<(string Key, string Direction)> DefaultSort { get; } = [("Name", "asc"), ("Id", "asc")];

    public Expression<Func<SupplyList, dynamic>> GetPropertyExpression(string propertyName)
    {
        return propertyName switch
        {
            "CreatedDate" => l => l.CreatedDate,
            "LastModifiedDate" => l => l.LastModifiedDate,
            "Name" => l => l.Name,
            "Frequency" => l => l.Frequency,
            "Id" => l => l.Id,
            _ => l => l.Id
        };
    }
}
