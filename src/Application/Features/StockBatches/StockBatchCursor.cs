using skestock.Application.Common.Keyset;
using skestock.Domain.Entities.Stock;

namespace skestock.Application.Features.StockBatches;

/// <summary>
/// Sort-key columns of a <see cref="StockBatch"/> row, projected alongside the list DTO so the
/// next cursor can be encoded without loading the full entity.
/// </summary>
public sealed record StockBatchCursor(Guid Id, DateOnly ReceivedDate, DateTimeOffset CreatedDate, DateTimeOffset LastModifiedDate) : IKeysetCursor
{
    public object? GetValue(string propertyName) => propertyName switch
    {
        nameof(Id) => Id,
        nameof(ReceivedDate) => ReceivedDate,
        nameof(CreatedDate) => CreatedDate,
        nameof(LastModifiedDate) => LastModifiedDate,
        _ => null
    };
}
