using skestock.Application.Common.Security;
using skestock.Application.Features.GoodsReceipts.Models;

namespace skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;

// Requires an authenticated user: GoodsReceiptImport.UploadedByUserId (who uploaded the file to
// import) is a required field distinct from the CreatedBy audit column, so an anonymous caller
// can't be allowed through to the handler.
[Authorize]
public class CreateGoodsReceiptImportCommand : IRequest<Result<GoodsReceiptImportDto>>
{
    public Guid ClassId { get; init; }
    public Guid FileMetadataId { get; init; }
}
