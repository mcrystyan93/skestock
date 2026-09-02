using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SchoolClasses;

public class SchoolClassFilterConfiguration : IFilterConfiguration<SchoolClass>
{
    public IReadOnlyDictionary<string, FilterField<SchoolClass>> Fields { get; } = new Dictionary<string, FilterField<SchoolClass>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<SchoolClass, Guid>>)(d => d.Id), typeof(Guid)),
        ["name"] = new((Expression<Func<SchoolClass, string>>)(d => d.Name), typeof(string)),
        ["status"] = new((Expression<Func<SchoolClass, ClassStatus>>)(d => d.Status), typeof(ClassStatus)),
        ["startDate"] = new((Expression<Func<SchoolClass, DateOnly>>)(d => d.StartDate), typeof(DateOnly)),
        ["endDate"] = new((Expression<Func<SchoolClass, DateOnly>>)(d => d.EndDate), typeof(DateOnly)),
        ["createdDate"] = new((Expression<Func<SchoolClass, DateTimeOffset>>)(d => d.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<SchoolClass, DateTimeOffset>>)(d => d.LastModifiedDate), typeof(DateTimeOffset))
    };
}
