using System.Linq.Expressions;
using skestock.Application.Common.Keyset;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Categories;

public sealed class CategorySortConfiguration : IKeysetSortConfiguration<Category>
{
    public IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = ["Name", "Id"],
            ["createdDate"] = ["Created", "Id"],
            ["lastModifiedDate"] = ["LastModified", "Id"],
            ["id"] = ["Id"]
        };

    public List<(string Key, string Direction)> DefaultSort { get; } = [("Created", "desc"), ("Id", "desc")];

    public Expression<Func<Category, dynamic>> GetPropertyExpression(string propertyName)
    {
        return propertyName switch
        {
            "Name" => d => d.Name,
            "Created" => d => d.CreatedDate,
            "LastModified" => d => d.LastModifiedDate,
            "Id" => d => d.Id,
            _ => d => d.Id
        };
    }

    public object? GetPropertyValue(Category entity, string propertyName)
    {
        return propertyName switch
        {
            "Name" => entity.Name,
            "Created" => entity.CreatedDate,
            "LastModified" => entity.LastModifiedDate,
            "Id" => entity.Id,
            _ => null
        };
    }
}
