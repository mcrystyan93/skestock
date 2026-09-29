using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SupplyLists;

public class SupplyListFilterConfiguration : IFilterConfiguration<SupplyList>
{
    public IReadOnlyDictionary<string, FilterField<SupplyList>> Fields { get; } = new Dictionary<string, FilterField<SupplyList>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<SupplyList, Guid>>)(l => l.Id), typeof(Guid)),
        ["name"] = new((Expression<Func<SupplyList, string>>)(l => l.Name), typeof(string)),
        ["frequency"] = new((Expression<Func<SupplyList, SupplyListFrequency>>)(l => l.Frequency), typeof(SupplyListFrequency)),
        ["isActive"] = new((Expression<Func<SupplyList, bool>>)(l => l.IsActive), typeof(bool)),
        ["createdDate"] = new((Expression<Func<SupplyList, DateTimeOffset>>)(l => l.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<SupplyList, DateTimeOffset>>)(l => l.LastModifiedDate), typeof(DateTimeOffset))
    };
}
