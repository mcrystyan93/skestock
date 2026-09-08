using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Models;

/// <summary>
/// The AI extraction for a single category import: the suggested category names for the reviewer to
/// edit/confirm. Existing categories (matched case-insensitively by name) are flagged so the UI can
/// indicate which suggestions would create a new category on confirm. Drives the review screen.
/// </summary>
public record CategoryImportReviewDto
{
    public Guid Id { get; init; }
    public CategoryImportStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public List<CategoryImportReviewLineDto> Suggestions { get; init; } = [];
}

public record CategoryImportReviewLineDto
{
    /// <summary>The suggested category name exactly as the AI read it off the document.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>True when a category with this name (case-insensitive) already exists.</summary>
    public bool AlreadyExists { get; init; }
}
