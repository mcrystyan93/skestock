using skestock.Domain.Enums;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.Items.Models;

/// <summary>
/// The merged AI extraction for an item import batch: the suggested items for the reviewer to
/// edit/confirm, gathered from every file in the batch in one request. Existing categories/items are
/// matched against the existing catalog for reviewer confirmation.
/// </summary>
public record ItemImportBatchReviewDto
{
    public Guid Id { get; init; }
    public ItemImportBatchStatus Status { get; init; }
    public Guid? ClientRequestId { get; init; }
    public int AttemptCount { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
    public List<ItemImportBatchFileDto> Files { get; init; } = [];
    public List<ImportBatchHistoryDto> History { get; init; } = [];
    public List<ItemImportBatchReviewLineDto> Suggestions { get; init; } = [];
}
