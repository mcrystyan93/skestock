using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Models;

/// <summary>
/// The AI extraction for a single item import: the suggested items for the reviewer to edit/confirm.
/// Existing categories (matched case-insensitively by name) and existing items (matched by SKU or
/// unique name/category) are returned so the UI can select exact catalog records before confirmation.
/// </summary>
public record ItemImportReviewDto
{
    public Guid Id { get; init; }
    public ItemImportStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public List<ItemImportReviewLineDto> Suggestions { get; init; } = [];
}

public record ItemImportReviewLineDto
{
    /// <summary>The suggested SKU exactly as the AI read it off the document, if any.</summary>
    public string? Sku { get; init; }

    /// <summary>The suggested item name exactly as the AI read it off the document.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The suggested category name this item belongs to.</summary>
    public string CategoryName { get; init; } = string.Empty;

    public string Unit { get; init; } = "unit";
    public string? Description { get; init; }
    public bool IsPerishable { get; init; }

    /// <summary>True when this suggestion has an unambiguous catalog item match.</summary>
    public bool ItemAlreadyExists { get; init; }

    /// <summary>True when a category with this name (case-insensitive) already exists.</summary>
    public bool CategoryAlreadyExists { get; init; }

    /// <summary>The catalog category matched by the extracted category name, or null when no match exists.</summary>
    public ItemImportReviewCategoryDto? MatchedCategory { get; init; }

    /// <summary>The catalog item matched by SKU or unique name/category, or null when no match exists.</summary>
    public ItemImportReviewItemDto? MatchedItem { get; init; }
}

public record ItemImportReviewCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record ItemImportReviewItemDto
{
    public Guid Id { get; init; }
    public string? Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Unit { get; init; } = "unit";
    public bool IsPerishable { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
}
