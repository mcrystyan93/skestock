using skestock.Domain.Entities.Storage;

namespace skestock.Domain.Entities.Items;

/// <summary>
/// One file included in an <see cref="ItemImportBatch"/>. Links the batch to the previously
/// uploaded/confirmed <see cref="Storage.FileMetadata"/> row and preserves the client's upload order.
/// </summary>
public class ItemImportBatchFile : BaseAuditableEntity
{
    public Guid ItemImportBatchId { get; set; }
    public ItemImportBatch Batch { get; set; } = null!;

    public Guid FileMetadataId { get; set; }
    public FileMetadata FileMetadata { get; set; } = null!;

    public int SortOrder { get; set; }
}
