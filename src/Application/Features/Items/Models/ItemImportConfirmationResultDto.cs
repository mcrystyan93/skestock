using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Models;

/// <summary>
/// Result of confirming an item import: the items (and categories) the reviewed list resolved to.
/// Persisted on the import (serialized) so a repeated confirmation returns the same payload
/// idempotently instead of creating items/categories again.
/// </summary>
public record ItemImportConfirmationResultDto
{
    public Guid ImportId { get; init; }
    public ItemImportStatus Status { get; init; }
    public List<ItemImportResultItemDto> Items { get; init; } = [];
}

public record ItemImportResultItemDto
{
    public Guid Id { get; init; }
    public string? Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>True when this item was created by the confirmation; false when an item with the same SKU already existed and was reused.</summary>
    public bool Created { get; init; }

    /// <summary>True when the item's category was created by the confirmation; false when it already existed.</summary>
    public bool CategoryCreated { get; init; }
}
