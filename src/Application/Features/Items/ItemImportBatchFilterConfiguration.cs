using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Items;

public sealed class ItemImportBatchFilterConfiguration : IFilterConfiguration<ItemImportBatch>
{
    public IReadOnlyDictionary<string, FilterField<ItemImportBatch>> Fields { get; } =
        new Dictionary<string, FilterField<ItemImportBatch>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = new((Expression<Func<ItemImportBatch, Guid>>)(batch => batch.Id), typeof(Guid)),
            ["status"] = new((Expression<Func<ItemImportBatch, ItemImportBatchStatus>>)(batch => batch.Status),
                typeof(ItemImportBatchStatus)),
            ["uploadedAt"] = new((Expression<Func<ItemImportBatch, DateTimeOffset>>)(batch => batch.UploadedAt),
                typeof(DateTimeOffset)),
            ["createdDate"] = new((Expression<Func<ItemImportBatch, DateTimeOffset>>)(batch => batch.CreatedDate),
                typeof(DateTimeOffset)),
            ["lastModifiedDate"] = new(
                (Expression<Func<ItemImportBatch, DateTimeOffset>>)(batch => batch.LastModifiedDate),
                typeof(DateTimeOffset))
        };
}
