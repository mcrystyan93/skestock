using skestock.Application.Common.Keyset;
using skestock.Domain.Entities.Categories;

namespace skestock.Application.Features.Categories;

/// <summary>
/// Sort-key columns of a <see cref="Category"/> row, projected alongside the list DTO so the
/// next cursor can be encoded without loading the full entity.
/// </summary>
public sealed record CategoryCursor(Guid Id, string Name, DateTimeOffset CreatedDate, DateTimeOffset LastModifiedDate) : IKeysetCursor
{
    public object? GetValue(string propertyName) => propertyName switch
    {
        nameof(Id) => Id,
        nameof(Name) => Name,
        nameof(CreatedDate) => CreatedDate,
        nameof(LastModifiedDate) => LastModifiedDate,
        _ => null
    };
}
