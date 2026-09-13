using skestock.Application.Common.Security;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Commands.CreateItemImportBatch;

// Requires an authenticated user: ItemImportBatch.UploadedByUserId (who uploaded the files to import)
// is a required field distinct from the CreatedBy audit column, so an anonymous caller can't be
// allowed through to the handler.
[Authorize]
public class CreateItemImportBatchCommand : IRequest<Result<ItemImportBatchDto>>
{
    public List<Guid> FileMetadataIds { get; init; } = [];

    /// <summary>
    /// Optional client-supplied idempotency key. Retrying the same create request with the same key
    /// (for the same caller) returns the already-created batch instead of creating a duplicate.
    /// </summary>
    public Guid? ClientRequestId { get; init; }
}
