using System.Linq.Expressions;
using skestock.Application.Common.Keyset;
using skestock.Domain.Entities;

namespace skestock.Application.Features.OrderLists;

public sealed class OrderListSortConfiguration : IKeysetSortConfiguration<OrderList>
{
    public IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["createdDate"] = ["CreatedDate", "Id"],
            ["lastModifiedDate"] = ["LastModifiedDate", "Id"],
            ["name"] = ["Name", "Id"],
            ["status"] = ["Status", "Id"],
            ["id"] = ["Id"]
        };

    public List<(string Key, string Direction)> DefaultSort { get; } = [("CreatedDate", "desc"), ("Id", "desc")];

    public Expression<Func<OrderList, dynamic>> GetPropertyExpression(string propertyName)
    {
        return propertyName switch
        {
            "CreatedDate" => o => o.CreatedDate,
            "LastModifiedDate" => o => o.LastModifiedDate,
            "Name" => o => o.Name!,
            "Status" => o => o.Status,
            "Id" => o => o.Id,
            _ => o => o.Id
        };
    }

    public object? GetPropertyValue(OrderList entity, string propertyName)
    {
        return propertyName switch
        {
            "CreatedDate" => entity.CreatedDate,
            "LastModifiedDate" => entity.LastModifiedDate,
            "Name" => entity.Name,
            "Status" => entity.Status,
            "Id" => entity.Id,
            _ => null
        };
    }
}
