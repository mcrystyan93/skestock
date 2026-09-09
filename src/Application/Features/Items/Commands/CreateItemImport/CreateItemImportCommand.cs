using skestock.Application.Common.Security;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Commands.CreateItemImport;

// Requires an authenticated user: ItemImport.UploadedByUserId (who uploaded the file to import) is a
// required field distinct from the CreatedBy audit column, so an anonymous caller can't be allowed
// through to the handler.
[Authorize]
public class CreateItemImportCommand : IRequest<Result<ItemImportDto>>
{
    public Guid FileMetadataId { get; init; }
}
