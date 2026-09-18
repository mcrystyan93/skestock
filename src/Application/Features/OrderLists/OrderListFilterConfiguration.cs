using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;

namespace skestock.Application.Features.OrderLists;

public class OrderListFilterConfiguration : IFilterConfiguration<OrderList>
{
    public IReadOnlyDictionary<string, FilterField<OrderList>> Fields { get; } = new Dictionary<string, FilterField<OrderList>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<OrderList, Guid>>)(o => o.Id), typeof(Guid)),
        ["classId"] = new((Expression<Func<OrderList, Guid>>)(o => o.ClassId), typeof(Guid)),
        ["status"] = new((Expression<Func<OrderList, int>>)(o => (int)o.Status), typeof(int)),
        ["createdDate"] = new((Expression<Func<OrderList, DateTimeOffset>>)(o => o.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<OrderList, DateTimeOffset>>)(o => o.LastModifiedDate), typeof(DateTimeOffset))
    };
}
