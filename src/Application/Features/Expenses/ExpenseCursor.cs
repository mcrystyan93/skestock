using skestock.Application.Common.Keyset;
using skestock.Domain.Entities.Expenses;

namespace skestock.Application.Features.Expenses;

/// <summary>
/// Sort-key columns of a <see cref="Expense"/> row, projected alongside the list DTO so the
/// next cursor can be encoded without loading the full entity.
/// </summary>
public sealed record ExpenseCursor(Guid Id, DateTimeOffset UploadedAt, DateTimeOffset CreatedDate, DateTimeOffset LastModifiedDate) : IKeysetCursor
{
    public object? GetValue(string propertyName) => propertyName switch
    {
        nameof(Id) => Id,
        nameof(UploadedAt) => UploadedAt,
        nameof(CreatedDate) => CreatedDate,
        nameof(LastModifiedDate) => LastModifiedDate,
        _ => null
    };
}
