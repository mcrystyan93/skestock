using System.Linq.Expressions;
using skestock.Application.Common.Keyset;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Expenses;

namespace skestock.Application.Features.Expenses;

public sealed class ExpenseSortConfiguration : IKeysetSortConfiguration<Expense>
{
    public IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["uploadedAt"] = ["UploadedAt", "Id"],
            ["createdDate"] = ["CreatedDate", "Id"],
            ["lastModifiedDate"] = ["LastModifiedDate", "Id"],
            ["id"] = ["Id"]
        };

    public List<(string Key, string Direction)> DefaultSort { get; } = [("CreatedDate", "desc"), ("Id", "desc")];

    public Expression<Func<Expense, dynamic>> GetPropertyExpression(string propertyName)
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
}
