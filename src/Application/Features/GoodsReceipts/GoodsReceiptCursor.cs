using skestock.Application.Common.Keyset;
using skestock.Domain.Entities.GoodsReceipts;

namespace skestock.Application.Features.GoodsReceipts;

/// <summary>
/// Sort-key columns of a <see cref="GoodsReceipt"/> row, projected alongside the list DTO so the
/// next cursor can be encoded without loading the full entity.
/// </summary>
public sealed record GoodsReceiptCursor(Guid Id, DateTimeOffset ReceivedAt, DateTimeOffset CreatedDate, DateTimeOffset LastModifiedDate) : IKeysetCursor
{
    public object? GetValue(string propertyName) => propertyName switch
    {
        nameof(Id) => Id,
        nameof(ReceivedAt) => ReceivedAt,
        nameof(CreatedDate) => CreatedDate,
        nameof(LastModifiedDate) => LastModifiedDate,
        _ => null
    };
}
