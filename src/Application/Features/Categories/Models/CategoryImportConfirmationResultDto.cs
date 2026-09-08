using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Models;

/// <summary>
/// Result of confirming a category import: the categories the reviewed list resolved to. Persisted on
/// the import (serialized) so a repeated confirmation returns the same payload idempotently instead of
/// creating categories again.
/// </summary>
public record CategoryImportConfirmationResultDto
{
    public Guid ImportId { get; init; }
    public CategoryImportStatus Status { get; init; }
    public List<CategoryImportResultCategoryDto> Categories { get; init; } = [];
}

public record CategoryImportResultCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;

    /// <summary>True when this category was created by the confirmation; false when it already existed.</summary>
    public bool Created { get; init; }
}
