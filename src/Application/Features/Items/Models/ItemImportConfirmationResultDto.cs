using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Models;

/// <summary>
/// Result of confirming an item import: the selected catalog items the reviewed list resolved to.
/// Persisted on the import (serialized) so a repeated confirmation returns the same payload
/// idempotently.
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
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>Retained for response compatibility; item creation happens before confirmation.</summary>
    public bool Created { get; init; }

    /// <summary>Retained for response compatibility; category creation happens before confirmation.</summary>
    public bool CategoryCreated { get; init; }
}
