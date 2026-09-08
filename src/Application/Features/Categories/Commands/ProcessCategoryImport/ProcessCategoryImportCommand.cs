namespace skestock.Application.Features.Categories.Commands.ProcessCategoryImport;

public class ProcessCategoryImportCommand(Guid categoryImportId) : IRequest<Result>
{
    public Guid CategoryImportId { get; init; } = categoryImportId;
}
