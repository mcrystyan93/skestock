using skestock.Application.Common.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Models;

public record CategoryImportBatchDto
{
    public Guid Id { get; init; }
    public CategoryImportBatchStatus Status { get; init; }
    public Guid? ClientRequestId { get; init; }
    public int AttemptCount { get; init; }
    public List<CategoryImportBatchFileDto> Files { get; init; } = [];
    public List<ImportBatchHistoryDto> History { get; init; } = [];
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
