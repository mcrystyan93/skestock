namespace skestock.Application.Features.Items.Commands.ProcessItemImport;

public class ProcessItemImportCommand(Guid itemImportId) : IRequest<Result>
{
    public Guid ItemImportId { get; init; } = itemImportId;
}
