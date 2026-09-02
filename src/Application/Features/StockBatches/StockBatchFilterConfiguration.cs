using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;

namespace skestock.Application.Features.StockBatches;

public class StockBatchFilterConfiguration : IFilterConfiguration<StockBatch>
{
    public IReadOnlyDictionary<string, FilterField<StockBatch>> Fields { get; } = new Dictionary<string, FilterField<StockBatch>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<StockBatch, Guid>>)(b => b.Id), typeof(Guid)),
        ["itemId"] = new((Expression<Func<StockBatch, Guid>>)(b => b.ItemId), typeof(Guid)),
        ["locationId"] = new((Expression<Func<StockBatch, Guid>>)(b => b.LocationId), typeof(Guid)),
        ["goodsReceiptId"] = new((Expression<Func<StockBatch, Guid?>>)(b => b.GoodsReceiptId), typeof(Guid)),
        ["receivedClassId"] = new((Expression<Func<StockBatch, Guid>>)(b => b.ReceivedClassId), typeof(Guid)),
        ["receivedDate"] = new((Expression<Func<StockBatch, DateOnly>>)(b => b.ReceivedDate), typeof(DateOnly)),
        ["expiryDate"] = new((Expression<Func<StockBatch, DateOnly?>>)(b => b.ExpiryDate), typeof(DateOnly)),
        ["createdDate"] = new((Expression<Func<StockBatch, DateTimeOffset>>)(b => b.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<StockBatch, DateTimeOffset>>)(b => b.LastModifiedDate), typeof(DateTimeOffset))
    };
}
