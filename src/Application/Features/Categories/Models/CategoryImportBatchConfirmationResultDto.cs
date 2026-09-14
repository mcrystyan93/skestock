using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Models;

public record CategoryImportBatchConfirmationResultDto
{
    public Guid BatchId { get; init; }
    public CategoryImportBatchStatus Status { get; init; }
    public List<CategoryImportBatchResultCategoryDto> Categories { get; init; } = [];
}

public record CategoryImportBatchResultCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool Created { get; init; }
}
