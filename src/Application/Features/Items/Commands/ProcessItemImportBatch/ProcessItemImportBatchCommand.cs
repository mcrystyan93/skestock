namespace skestock.Application.Features.Items.Commands.ProcessItemImportBatch;

public class ProcessItemImportBatchCommand(Guid itemImportBatchId) : IRequest<Result>
{
    public Guid ItemImportBatchId { get; init; } = itemImportBatchId;
}
