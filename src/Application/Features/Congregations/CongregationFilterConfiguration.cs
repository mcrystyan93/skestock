using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities.Congregations;

namespace skestock.Application.Features.Congregations;

public class CongregationFilterConfiguration : IFilterConfiguration<Congregation>
{
    public IReadOnlyDictionary<string, FilterField<Congregation>> Fields { get; } = new Dictionary<string, FilterField<Congregation>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<Congregation, Guid>>)(d => d.Id), typeof(Guid)),
        ["name"] = new((Expression<Func<Congregation, string>>)(d => d.Name), typeof(string)),
        ["canReceiveStudents"] = new((Expression<Func<Congregation, bool>>)(d => d.CanReceiveStudents), typeof(bool)),
        ["isSmallCongregation"] = new((Expression<Func<Congregation, bool>>)(d => d.IsSmallCongregation), typeof(bool)),
        ["createdDate"] = new((Expression<Func<Congregation, DateTimeOffset>>)(d => d.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<Congregation, DateTimeOffset>>)(d => d.LastModifiedDate), typeof(DateTimeOffset))
    };
}
