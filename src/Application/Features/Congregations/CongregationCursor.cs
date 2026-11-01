using skestock.Application.Common.Keyset;
using skestock.Domain.Entities.Congregations;

namespace skestock.Application.Features.Congregations;

/// <summary>
/// Sort-key columns of a <see cref="Congregation"/> row, projected alongside the list DTO so the
/// next cursor can be encoded without loading the full entity.
/// </summary>
public sealed record CongregationCursor(Guid Id, string Name, DateTimeOffset CreatedDate, DateTimeOffset LastModifiedDate) : IKeysetCursor
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
