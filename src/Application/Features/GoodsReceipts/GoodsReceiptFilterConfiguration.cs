using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;

namespace skestock.Application.Features.GoodsReceipts;

public class GoodsReceiptFilterConfiguration : IFilterConfiguration<GoodsReceipt>
{
    public IReadOnlyDictionary<string, FilterField<GoodsReceipt>> Fields { get; } = new Dictionary<string, FilterField<GoodsReceipt>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<GoodsReceipt, Guid>>)(r => r.Id), typeof(Guid)),
        ["classId"] = new((Expression<Func<GoodsReceipt, Guid>>)(r => r.ClassId), typeof(Guid)),
        ["receivedAt"] = new((Expression<Func<GoodsReceipt, DateTime>>)(r => r.ReceivedAt), typeof(DateTime)),
        ["supplierReference"] = new((Expression<Func<GoodsReceipt, string?>>)(r => r.SupplierReference), typeof(string)),
        ["createdDate"] = new((Expression<Func<GoodsReceipt, DateTimeOffset>>)(r => r.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<GoodsReceipt, DateTimeOffset>>)(r => r.LastModifiedDate), typeof(DateTimeOffset))
    };
}
