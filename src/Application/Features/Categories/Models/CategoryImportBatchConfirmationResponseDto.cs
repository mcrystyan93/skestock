using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Models;

public record CategoryImportBatchConfirmationResponseDto
{
    public Guid BatchId { get; init; }
    public CategoryImportBatchStatus Status { get; init; }
}
