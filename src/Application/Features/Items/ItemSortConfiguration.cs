using System.Linq.Expressions;
using skestock.Application.Common.Keyset;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Items;

public sealed class ItemSortConfiguration : IKeysetSortConfiguration<Item>
{
    public IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = ["Name", "Id"],
            ["sku"] = ["Sku", "Id"],
            ["unit"] = ["Unit", "Id"],
            ["createdDate"] = ["Created", "Id"],
            ["lastModifiedDate"] = ["LastModified", "Id"],
            ["id"] = ["Id"]
        };

    public List<(string Key, string Direction)> DefaultSort { get; } = [("Created", "desc"), ("Id", "desc")];

    public Expression<Func<Item, dynamic>> GetPropertyExpression(string propertyName)
    {
        return propertyName switch
        {
            "Name" => d => d.Name,
            "Sku" => d => d.Sku!,
            "Unit" => d => d.Unit,
            "Created" => d => d.CreatedDate,
            "LastModified" => d => d.LastModifiedDate,
            "Id" => d => d.Id,
            _ => d => d.Id
        };
    }

    public object? GetPropertyValue(Item entity, string propertyName)
    {
        return propertyName switch
        {
            "Name" => entity.Name,
            "Sku" => entity.Sku,
            "Unit" => entity.Unit,
            "Created" => entity.CreatedDate,
            "LastModified" => entity.LastModifiedDate,
            "Id" => entity.Id,
            _ => null
        };
    }
}
