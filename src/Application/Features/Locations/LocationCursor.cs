using skestock.Application.Common.Keyset;

namespace skestock.Application.Features.Locations;

/// <summary>
/// Sort-key columns of a <see cref="Domain.Entities.Location"/> row, projected alongside the list DTO so the
/// next cursor can be encoded without loading the full entity.
/// </summary>
public sealed record LocationCursor(Guid Id, string Name, string Type, DateTimeOffset CreatedDate, DateTimeOffset LastModifiedDate) : IKeysetCursor
{
    public object? GetValue(string propertyName) => propertyName switch
    {
        nameof(Id) => Id,
        nameof(Name) => Name,
        nameof(Type) => Type,
        nameof(CreatedDate) => CreatedDate,
        nameof(LastModifiedDate) => LastModifiedDate,
        _ => null
    };
}
