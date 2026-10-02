using skestock.Application.Common.Keyset;
using skestock.Domain.Entities.Items;

namespace skestock.Application.Features.Items;

/// <summary>
/// Sort-key columns of a <see cref="Item"/> row, projected alongside the list DTO so the
/// next cursor can be encoded without loading the full entity.
/// </summary>
public sealed record ItemCursor(Guid Id, string Name, string? Sku, string Unit, DateTimeOffset CreatedDate, DateTimeOffset LastModifiedDate) : IKeysetCursor
{
    public object? GetValue(string propertyName) => propertyName switch
    {
        nameof(Id) => Id,
        nameof(Name) => Name,
        nameof(Sku) => Sku,
        nameof(Unit) => Unit,
        nameof(CreatedDate) => CreatedDate,
        nameof(LastModifiedDate) => LastModifiedDate,
        _ => null
    };
}
