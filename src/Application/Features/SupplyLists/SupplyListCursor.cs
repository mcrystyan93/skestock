using skestock.Application.Common.Keyset;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SupplyLists;

public sealed record SupplyListCursor(Guid Id, string Name, SupplyListFrequency Frequency, DateTimeOffset CreatedDate, DateTimeOffset LastModifiedDate) : IKeysetCursor
{
    public object? GetValue(string propertyName) => propertyName switch
    {
        nameof(Id) => Id,
        nameof(Name) => Name,
        nameof(Frequency) => Frequency,
        nameof(CreatedDate) => CreatedDate,
        nameof(LastModifiedDate) => LastModifiedDate,
        _ => null
    };
}
