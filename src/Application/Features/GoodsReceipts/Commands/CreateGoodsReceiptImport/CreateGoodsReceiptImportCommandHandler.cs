using skestock.Application.Common.Interfaces;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;

public class CreateGoodsReceiptImportCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<CreateGoodsReceiptImportCommand, Result<GoodsReceiptImportDto>>
{
    public async ValueTask<Result<GoodsReceiptImportDto>> Handle(CreateGoodsReceiptImportCommand request, CancellationToken cancellationToken)
    {
        // AuthorizationBehaviour (earlier in the pipeline) guarantees an authenticated caller, so
        // IUser.Id (the Identity/AspNetUsers id) is expected to be populated.
        // GoodsReceiptImport.UploadedByUserId is a FK to UserProfile.IdentityId (not UserProfile.Id -
        // see GoodsReceiptImportConfiguration), so the raw identity id is stored directly.
        var identityId = Guard.Against.Null(user.Id, message: "Creating a goods receipt import requires an authenticated user.");

        // BlobPath is not supplied by the caller - it's the storage location recorded on the
        // already-uploaded file, so it's copied from the FileMetadata row (existence enforced by
        // the validator).
        var blobPath = await dbContext.FileMetadata
            .AsNoTracking()
            .Where(f => f.Id == request.FileMetadataId)
            .Select(f => f.BlobPath)
            .SingleAsync(cancellationToken);

        var import = GoodsReceiptImport.Create(request.ClassId, request.FileMetadataId, identityId, blobPath);

        dbContext.GoodsReceiptImports.Add(import);

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new GoodsReceiptImportDto
        {
            Id = import.Id,
            Status = import.Status,
            ClassId = import.ClassId,
            FileMetadataId = import.FileMetadataId,
            BlobPath = import.BlobPath,
            UploadedAt = import.UploadedAt
        };

        return Result.Ok(dto);
    }
}
