namespace skestock.Domain.Entities;

/// <summary>
/// One file included in a category import batch.
/// </summary>
public class CategoryImportBatchFile : BaseAuditableEntity
{
    public Guid CategoryImportBatchId { get; set; }
    public CategoryImportBatch Batch { get; set; } = null!;

    public Guid FileMetadataId { get; set; }
    public FileMetadata FileMetadata { get; set; } = null!;

    public int SortOrder { get; set; }
}
