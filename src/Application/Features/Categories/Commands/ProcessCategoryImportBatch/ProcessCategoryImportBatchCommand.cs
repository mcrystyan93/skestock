namespace skestock.Application.Features.Categories.Commands.ProcessCategoryImportBatch;

public class ProcessCategoryImportBatchCommand(Guid categoryImportBatchId) : IRequest<Result>
{
    public Guid CategoryImportBatchId { get; init; } = categoryImportBatchId;
}
