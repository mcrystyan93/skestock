using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories;

public class CategoryImportFilterConfiguration : IFilterConfiguration<CategoryImport>
{
    public IReadOnlyDictionary<string, FilterField<CategoryImport>> Fields { get; } = new Dictionary<string, FilterField<CategoryImport>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<CategoryImport, Guid>>)(i => i.Id), typeof(Guid)),
        ["status"] = new((Expression<Func<CategoryImport, CategoryImportStatus>>)(i => i.Status), typeof(CategoryImportStatus)),
        ["fileMetadataId"] = new((Expression<Func<CategoryImport, Guid>>)(i => i.FileMetadataId), typeof(Guid)),
        ["uploadedAt"] = new((Expression<Func<CategoryImport, DateTime>>)(i => i.UploadedAt), typeof(DateTime)),
        ["createdDate"] = new((Expression<Func<CategoryImport, DateTimeOffset>>)(i => i.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<CategoryImport, DateTimeOffset>>)(i => i.LastModifiedDate), typeof(DateTimeOffset))
    };
}
