using System.Linq.Expressions;
using skestock.Application.Common.Keyset;
using skestock.Domain.Entities;

namespace skestock.Application.Features.GoodsReceipts;

public sealed class GoodsReceiptSortConfiguration : IKeysetSortConfiguration<GoodsReceipt>
{
    public IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["receivedAt"] = ["ReceivedAt", "Id"],
            ["createdDate"] = ["CreatedDate", "Id"],
            ["lastModifiedDate"] = ["LastModified", "Id"],
            ["id"] = ["Id"]
        };

    public List<(string Key, string Direction)> DefaultSort { get; } = [("CreatedDate", "desc"), ("Id", "desc")];

    public Expression<Func<GoodsReceipt, dynamic>> GetPropertyExpression(string propertyName)
    {
        return propertyName switch
        {
            "ReceivedAt" => r => r.ReceivedAt,
            "CreatedDate" => r => r.CreatedDate,
            "LastModifiedDate" => r => r.LastModifiedDate,
            "Id" => r => r.Id,
            _ => r => r.Id
        };
    }

    public object? GetPropertyValue(GoodsReceipt entity, string propertyName)
    {
        return propertyName switch
        {
            "ReceivedAt" => entity.ReceivedAt,
            "CreatedDate" => entity.CreatedDate,
            "LastModifiedDate" => entity.LastModifiedDate,
            "Id" => entity.Id,
            _ => null
        };
    }
}
