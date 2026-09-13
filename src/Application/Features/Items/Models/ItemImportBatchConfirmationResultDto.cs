using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Models;

/// <summary>
/// Result of confirming an item import batch: the selected catalog items the reviewed list resolved
/// to. Persisted on the batch (serialized) so a repeated confirmation returns the same payload
/// idempotently.
/// </summary>
public record ItemImportBatchConfirmationResultDto
{
    public Guid BatchId { get; init; }
    public ItemImportBatchStatus Status { get; init; }
    public List<ItemImportBatchResultItemDto> Items { get; init; } = [];
}

public record ItemImportBatchResultItemDto
{
    public Guid Id { get; init; }
    public string? Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;

    public bool Created { get; init; }
    public bool CategoryCreated { get; init; }
}
