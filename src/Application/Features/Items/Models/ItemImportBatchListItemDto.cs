using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Models;

/// <summary>
/// Row shape for the paginated item-import-batch list used by the Items table.
/// </summary>
public record ItemImportBatchListItemDto
{
    public Guid Id { get; init; }
    public ItemImportBatchStatus Status { get; init; }
    public List<ItemImportBatchFileDto> Files { get; init; } = [];
    public string? ErrorMessage { get; init; }
    public string? UploadedByName { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
}
