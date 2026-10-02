using skestock.Application.Common.Keyset;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Enums;

namespace skestock.Application.Features.OrderLists;

/// <summary>
/// Sort-key columns of a <see cref="OrderList"/> row, projected alongside the list DTO so the
/// next cursor can be encoded without loading the full entity.
/// </summary>
public sealed record OrderListCursor(Guid Id, string? Name, OrderListStatus Status, DateTimeOffset CreatedDate, DateTimeOffset LastModifiedDate) : IKeysetCursor
{
    public object? GetValue(string propertyName) => propertyName switch
    {
        nameof(Id) => Id,
        nameof(Name) => Name,
        nameof(Status) => Status,
        nameof(CreatedDate) => CreatedDate,
        nameof(LastModifiedDate) => LastModifiedDate,
        _ => null
    };
}
