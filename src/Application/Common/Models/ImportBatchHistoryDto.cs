using skestock.Domain.Enums;

namespace skestock.Application.Common.Models;

public record ImportBatchHistoryDto
{
    public ImportBatchHistoryStatus Status { get; init; }
    public int Attempt { get; init; }
    public string? Message { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
