using skestock.Application.Common.Security;
using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Categories.Commands.CreateCategoryImportBatch;

[Authorize]
public class CreateCategoryImportBatchCommand : IRequest<Result<CategoryImportBatchDto>>
{
    public List<Guid> FileMetadataIds { get; init; } = [];
    public Guid? ClientRequestId { get; init; }
}
