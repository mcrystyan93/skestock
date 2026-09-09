using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Items;

public class ItemImportFilterConfiguration : IFilterConfiguration<ItemImport>
{
    public IReadOnlyDictionary<string, FilterField<ItemImport>> Fields { get; } = new Dictionary<string, FilterField<ItemImport>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<ItemImport, Guid>>)(i => i.Id), typeof(Guid)),
        ["status"] = new((Expression<Func<ItemImport, ItemImportStatus>>)(i => i.Status), typeof(ItemImportStatus)),
        ["fileMetadataId"] = new((Expression<Func<ItemImport, Guid>>)(i => i.FileMetadataId), typeof(Guid)),
        ["uploadedAt"] = new((Expression<Func<ItemImport, DateTime>>)(i => i.UploadedAt), typeof(DateTime)),
        ["createdDate"] = new((Expression<Func<ItemImport, DateTimeOffset>>)(i => i.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<ItemImport, DateTimeOffset>>)(i => i.LastModifiedDate), typeof(DateTimeOffset))
    };
}
