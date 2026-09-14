using System.Linq.Expressions;
using skestock.Application.Common.Keyset;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Categories;

public sealed class CategoryImportBatchSortConfiguration : IKeysetSortConfiguration<CategoryImportBatch>
{
    public IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["uploadedAt"] = ["UploadedAt", "Id"],
            ["createdDate"] = ["CreatedDate", "Id"],
            ["lastModifiedDate"] = ["LastModifiedDate", "Id"],
            ["id"] = ["Id"]
        };

    public List<(string Key, string Direction)> DefaultSort { get; } =
        [("CreatedDate", "desc"), ("Id", "desc")];

    public Expression<Func<CategoryImportBatch, dynamic>> GetPropertyExpression(string propertyName) =>
        propertyName switch
        {
            "UploadedAt" => batch => batch.UploadedAt,
            "CreatedDate" => batch => batch.CreatedDate,
            "LastModifiedDate" => batch => batch.LastModifiedDate,
            "Id" => batch => batch.Id,
            _ => batch => batch.Id
        };

    public object? GetPropertyValue(CategoryImportBatch entity, string propertyName) =>
        propertyName switch
        {
            "UploadedAt" => entity.UploadedAt,
            "CreatedDate" => entity.CreatedDate,
            "LastModifiedDate" => entity.LastModifiedDate,
            "Id" => entity.Id,
            _ => null
        };
}
