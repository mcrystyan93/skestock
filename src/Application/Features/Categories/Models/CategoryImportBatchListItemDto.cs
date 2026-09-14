using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Models;

public record CategoryImportBatchListItemDto
{
    public Guid Id { get; init; }
    public CategoryImportBatchStatus Status { get; init; }
    public List<CategoryImportBatchFileDto> Files { get; init; } = [];
    public string? ErrorMessage { get; init; }
    public string? UploadedByName { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
}
