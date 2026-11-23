using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Expenses;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Expenses;

public class ExpenseFilterConfiguration : IFilterConfiguration<Expense>
{
    public IReadOnlyDictionary<string, FilterField<Expense>> Fields { get; } = new Dictionary<string, FilterField<Expense>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<Expense, Guid>>)(i => i.Id), typeof(Guid)),
        ["classId"] = new((Expression<Func<Expense, Guid>>)(i => i.SchoolClassId), typeof(Guid)),
        ["status"] = new((Expression<Func<Expense, ExpenseStatus>>)(i => i.Status), typeof(ExpenseStatus)),
        ["source"] = new((Expression<Func<Expense, ExpenseSource>>)(i => i.Source), typeof(ExpenseSource)),
        ["uploadedAt"] = new((Expression<Func<Expense, DateTimeOffset>>)(i => i.UploadedAt), typeof(DateTimeOffset)),
        ["createdDate"] = new((Expression<Func<Expense, DateTimeOffset>>)(i => i.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<Expense, DateTimeOffset>>)(i => i.LastModifiedDate), typeof(DateTimeOffset))
    };
}
