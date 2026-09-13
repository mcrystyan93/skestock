namespace skestock.Application.Features.Items.Models;

public record ItemImportBatchReviewLineDto
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
    public ItemImportBatchReviewCategoryDto? MatchedCategory { get; init; }

    /// <summary>The catalog item matched by SKU or unique name/category, or null when no match exists.</summary>
    public ItemImportBatchReviewItemDto? MatchedItem { get; init; }
}

public record ItemImportBatchReviewCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record ItemImportBatchReviewItemDto
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
