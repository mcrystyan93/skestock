using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Commands.ProcessCategoryImport;
using skestock.Application.Features.Categories.Models;
using skestock.Application.Common.Errors;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Domain.Queues;
using skestock.Shared;
using System.Text.Json;

namespace skestock.Application.Features.Categories.Commands.CreateCategoryImport;

public class CreateCategoryImportCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<CreateCategoryImportCommand, Result<CategoryImportDto>>
{
    public async ValueTask<Result<CategoryImportDto>> Handle(CreateCategoryImportCommand request, CancellationToken cancellationToken)
    {
        // AuthorizationBehaviour (earlier in the pipeline) guarantees an authenticated caller, so
        // IUser.Id (the Identity/AspNetUsers id) is expected to be populated.
        // CategoryImport.UploadedByUserId is a FK to UserProfile.IdentityId (not UserProfile.Id -
        // see CategoryImportConfiguration), so the raw identity id is stored directly.
        var identityId = Guard.Against.Null(user.Id, message: "Creating a category import requires an authenticated user.");

        // BlobPath is not supplied by the caller - it's the storage location recorded on the
        // already-uploaded file, so it's copied from the FileMetadata row (existence enforced by
        // the validator).
        var file = await dbContext.FileMetadata
            .AsNoTracking()
            .Where(f => f.Id == request.FileMetadataId)
            .Select(f => new { f.BlobPath, f.Status })
            .SingleOrDefaultAsync(cancellationToken);

        if (file is null || file.Status != FileStatus.Completed)
            return Result.Fail(new CategoryImportErrors.FileMetadataNotConfirmed(request.FileMetadataId));

        var import = CategoryImport.Create(request.FileMetadataId, identityId, file.BlobPath);

        dbContext.CategoryImports.Add(import);

        // The outbox row must be atomic with the import row, so it is enqueued here inside the same
        // unit of work.
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(ProcessCategoryImportCommand).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new ProcessCategoryImportCommand(import.Id)),
            QueueName = Services.CategoryImportQueue,
            UserId = identityId
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new CategoryImportDto
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
