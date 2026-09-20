using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories;

public sealed class CategoryImportBatchFilterConfiguration : IFilterConfiguration<CategoryImportBatch>
{
    public IReadOnlyDictionary<string, FilterField<CategoryImportBatch>> Fields { get; } =
        new Dictionary<string, FilterField<CategoryImportBatch>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = new((Expression<Func<CategoryImportBatch, Guid>>)(batch => batch.Id), typeof(Guid)),
            ["status"] = new((Expression<Func<CategoryImportBatch, CategoryImportBatchStatus>>)(batch => batch.Status),
                typeof(CategoryImportBatchStatus)),
            ["uploadedAt"] = new((Expression<Func<CategoryImportBatch, DateTimeOffset>>)(batch => batch.UploadedAt),
                typeof(DateTimeOffset)),
            ["createdDate"] = new((Expression<Func<CategoryImportBatch, DateTimeOffset>>)(batch => batch.CreatedDate),
                typeof(DateTimeOffset)),
            ["lastModifiedDate"] = new(
                (Expression<Func<CategoryImportBatch, DateTimeOffset>>)(batch => batch.LastModifiedDate),
                typeof(DateTimeOffset))
        };
}
