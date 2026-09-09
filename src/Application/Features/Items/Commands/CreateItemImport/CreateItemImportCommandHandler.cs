using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Commands.ProcessItemImport;
using skestock.Application.Features.Items.Models;
using skestock.Application.Common.Errors;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Domain.Queues;
using skestock.Shared;
using System.Text.Json;

namespace skestock.Application.Features.Items.Commands.CreateItemImport;

public class CreateItemImportCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<CreateItemImportCommand, Result<ItemImportDto>>
{
    public async ValueTask<Result<ItemImportDto>> Handle(CreateItemImportCommand request, CancellationToken cancellationToken)
    {
        // AuthorizationBehaviour (earlier in the pipeline) guarantees an authenticated caller, so
        // IUser.Id (the Identity/AspNetUsers id) is expected to be populated.
        // ItemImport.UploadedByUserId is a FK to UserProfile.IdentityId (not UserProfile.Id - see
        // ItemImportConfiguration), so the raw identity id is stored directly.
        var identityId = Guard.Against.Null(user.Id, message: "Creating an item import requires an authenticated user.");

        // BlobPath is not supplied by the caller - it's the storage location recorded on the
        // already-uploaded file, so it's copied from the FileMetadata row (existence enforced by
        // the validator).
        var file = await dbContext.FileMetadata
            .AsNoTracking()
            .Where(f => f.Id == request.FileMetadataId)
            .Select(f => new { f.BlobPath, f.Status })
            .SingleOrDefaultAsync(cancellationToken);

        if (file is null || file.Status != FileStatus.Completed)
            return Result.Fail(new ItemImportErrors.FileMetadataNotConfirmed(request.FileMetadataId));

        var import = ItemImport.Create(request.FileMetadataId, identityId, file.BlobPath);

        dbContext.ItemImports.Add(import);

        // The outbox row must be atomic with the import row, so it is enqueued here inside the same
        // unit of work.
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(ProcessItemImportCommand).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new ProcessItemImportCommand(import.Id)),
            QueueName = Services.ItemImportQueue,
            UserId = identityId
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new ItemImportDto
        {
            Id = import.Id,
            Status = import.Status,
            FileMetadataId = import.FileMetadataId,
            BlobPath = import.BlobPath,
            UploadedAt = import.UploadedAt
        };

        return Result.Ok(dto);
    }
}
