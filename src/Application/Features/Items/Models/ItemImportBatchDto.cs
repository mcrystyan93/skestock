using skestock.Domain.Enums;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.Items.Models;

public record ItemImportBatchDto
{
    public Guid Id { get; init; }
    public ItemImportBatchStatus Status { get; init; }
    public Guid? ClientRequestId { get; init; }
    public int AttemptCount { get; init; }
    public List<ItemImportBatchFileDto> Files { get; init; } = [];
    public List<ImportBatchHistoryDto> History { get; init; } = [];
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
