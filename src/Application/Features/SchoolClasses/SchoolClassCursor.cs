using skestock.Application.Common.Keyset;

namespace skestock.Application.Features.SchoolClasses;

/// <summary>
/// Sort-key columns of a <see cref="Domain.Entities.SchoolClass"/> row, projected alongside the list DTO so the
/// next cursor can be encoded without loading the full entity.
/// </summary>
public sealed record SchoolClassCursor(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, DateTimeOffset CreatedDate, DateTimeOffset LastModifiedDate) : IKeysetCursor
{
    public object? GetValue(string propertyName) => propertyName switch
    {
        nameof(Id) => Id,
        nameof(Name) => Name,
        nameof(StartDate) => StartDate,
        nameof(EndDate) => EndDate,
        nameof(CreatedDate) => CreatedDate,
        nameof(LastModifiedDate) => LastModifiedDate,
        _ => null
    };
}
