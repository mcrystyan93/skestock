using skestock.Application.Common.Security;
using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Categories.Commands.CreateCategoryImport;

// Requires an authenticated user: CategoryImport.UploadedByUserId (who uploaded the file to import)
// is a required field distinct from the CreatedBy audit column, so an anonymous caller can't be
// allowed through to the handler.
[Authorize]
public class CreateCategoryImportCommand : IRequest<Result<CategoryImportDto>>
{
    public Guid FileMetadataId { get; init; }
}
