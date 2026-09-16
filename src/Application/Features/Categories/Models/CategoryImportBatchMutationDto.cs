using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Models;

public record CategoryImportBatchMutationDto
{
    public Guid Id { get; init; }
    public CategoryImportBatchStatus Status { get; init; }
}
