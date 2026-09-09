using System.Linq.Expressions;
using skestock.Application.Common.Keyset;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Items;

public sealed class ItemImportSortConfiguration : IKeysetSortConfiguration<ItemImport>
{
    public IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["uploadedAt"] = ["UploadedAt", "Id"],
            ["createdDate"] = ["CreatedDate", "Id"],
            ["lastModifiedDate"] = ["LastModified", "Id"],
            ["id"] = ["Id"]
        };

    public List<(string Key, string Direction)> DefaultSort { get; } = [("CreatedDate", "desc"), ("Id", "desc")];

    public Expression<Func<ItemImport, dynamic>> GetPropertyExpression(string propertyName)
    {
        return propertyName switch
        {
            "UploadedAt" => i => i.UploadedAt,
            "CreatedDate" => i => i.CreatedDate,
            "LastModifiedDate" => i => i.LastModifiedDate,
            "Id" => i => i.Id,
            _ => i => i.Id
        };
    }

    public object? GetPropertyValue(ItemImport entity, string propertyName)
    {
        return propertyName switch
        {
            "UploadedAt" => entity.UploadedAt,
            "CreatedDate" => entity.CreatedDate,
            "LastModifiedDate" => entity.LastModifiedDate,
            "Id" => entity.Id,
            _ => null
        };
    }
}
