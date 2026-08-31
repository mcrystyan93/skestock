using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;

namespace skestock.Application.Features.StockBatches;

public class StockBatchFilterConfiguration : IFilterConfiguration<StockBatch>
{
    public IReadOnlyDictionary<string, FilterField<StockBatch>> Fields { get; } = new Dictionary<string, FilterField<StockBatch>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<StockBatch, int>>)(b => b.Id), typeof(int)),
        ["itemId"] = new((Expression<Func<StockBatch, int>>)(b => b.ItemId), typeof(int)),
        ["locationId"] = new((Expression<Func<StockBatch, int>>)(b => b.LocationId), typeof(int)),
        ["goodsReceiptId"] = new((Expression<Func<StockBatch, int?>>)(b => b.GoodsReceiptId), typeof(int)),
        ["receivedClassId"] = new((Expression<Func<StockBatch, int>>)(b => b.ReceivedClassId), typeof(int)),
        ["receivedDate"] = new((Expression<Func<StockBatch, DateOnly>>)(b => b.ReceivedDate), typeof(DateOnly)),
        ["expiryDate"] = new((Expression<Func<StockBatch, DateOnly?>>)(b => b.ExpiryDate), typeof(DateOnly)),
        ["createdDate"] = new((Expression<Func<StockBatch, DateTimeOffset>>)(b => b.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<StockBatch, DateTimeOffset>>)(b => b.LastModifiedDate), typeof(DateTimeOffset))
    };
}
