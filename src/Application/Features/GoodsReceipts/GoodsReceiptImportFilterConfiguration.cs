using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.GoodsReceipts;

public class GoodsReceiptImportFilterConfiguration : IFilterConfiguration<GoodsReceiptImport>
{
    public IReadOnlyDictionary<string, FilterField<GoodsReceiptImport>> Fields { get; } = new Dictionary<string, FilterField<GoodsReceiptImport>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<GoodsReceiptImport, Guid>>)(i => i.Id), typeof(Guid)),
        ["classId"] = new((Expression<Func<GoodsReceiptImport, Guid>>)(i => i.ClassId), typeof(Guid)),
        ["status"] = new((Expression<Func<GoodsReceiptImport, GoodsReceiptImportStatus>>)(i => i.Status), typeof(GoodsReceiptImportStatus)),
        ["fileMetadataId"] = new((Expression<Func<GoodsReceiptImport, Guid>>)(i => i.FileMetadataId), typeof(Guid)),
        ["uploadedAt"] = new((Expression<Func<GoodsReceiptImport, DateTime>>)(i => i.UploadedAt), typeof(DateTime)),
        ["createdDate"] = new((Expression<Func<GoodsReceiptImport, DateTimeOffset>>)(i => i.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<GoodsReceiptImport, DateTimeOffset>>)(i => i.LastModifiedDate), typeof(DateTimeOffset))
    };
}
