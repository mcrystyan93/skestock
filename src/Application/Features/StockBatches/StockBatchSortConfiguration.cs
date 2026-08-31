using System.Linq.Expressions;
using skestock.Application.Common.Keyset;
using skestock.Domain.Entities;

namespace skestock.Application.Features.StockBatches;

public sealed class StockBatchSortConfiguration : IKeysetSortConfiguration<StockBatch>
{
    public IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["receivedDate"] = ["ReceivedDate", "Id"],
            ["createdDate"] = ["CreatedDate", "Id"],
            ["lastModifiedDate"] = ["LastModified", "Id"],
            ["id"] = ["Id"]
        };

    public List<(string Key, string Direction)> DefaultSort { get; } = [("CreatedDate", "desc"), ("Id", "desc")];

    public Expression<Func<StockBatch, dynamic>> GetPropertyExpression(string propertyName)
    {
        return propertyName switch
        {
            "ReceivedDate" => b => b.ReceivedDate,
            "CreatedDate" => b => b.CreatedDate,
            "LastModifiedDate" => b => b.LastModifiedDate,
            "Id" => b => b.Id,
            _ => b => b.Id
        };
    }

    public object? GetPropertyValue(StockBatch entity, string propertyName)
    {
        return propertyName switch
        {
            "ReceivedDate" => entity.ReceivedDate,
            "CreatedDate" => entity.CreatedDate,
            "LastModifiedDate" => entity.LastModifiedDate,
            "Id" => entity.Id,
            _ => null
        };
    }
}
