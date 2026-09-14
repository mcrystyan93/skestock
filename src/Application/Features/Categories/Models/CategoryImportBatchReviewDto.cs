using skestock.Application.Common.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Models;

public record CategoryImportBatchReviewDto
{
    public Guid Id { get; init; }
    public CategoryImportBatchStatus Status { get; init; }
    public Guid? ClientRequestId { get; init; }
    public int AttemptCount { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public List<CategoryImportBatchFileDto> Files { get; init; } = [];
    public List<ImportBatchHistoryDto> History { get; init; } = [];
    public List<CategoryImportReviewLineDto> Suggestions { get; init; } = [];
}

public record CategoryImportReviewLineDto
{
    public string Name { get; init; } = string.Empty;
    public bool AlreadyExists { get; init; }
    public CategoryImportReviewCategoryDto? MatchedCategory { get; init; }
}

public record CategoryImportReviewCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
