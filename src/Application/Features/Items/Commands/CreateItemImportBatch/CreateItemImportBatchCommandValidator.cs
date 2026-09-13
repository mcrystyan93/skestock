using FluentValidation.Results;
using Microsoft.Extensions.Options;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models.Options;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Commands.CreateItemImportBatch;

public class CreateItemImportBatchCommandValidator : AbstractValidator<CreateItemImportBatchCommand>
{
    private readonly ImportBatchOptions _limits;

    public CreateItemImportBatchCommandValidator(
        IApplicationDbContext dbContext, IOptions<ImportBatchOptions> importBatchOptions, IUser user)
    {
        _limits = importBatchOptions.Value;

        RuleFor(x => x.FileMetadataIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage("At least one file is required.")
            .Must(ids => ids is { Count: >= 1 })
            .WithErrorCode(ValidationErrorCodes.TooFewFiles)
            .WithMessage("At least one file is required for an item import batch.");

        RuleFor(x => x.FileMetadataIds)
            .Must(ids => ids is not null && ids.Count <= _limits.MaxFiles)
            .WithErrorCode(ValidationErrorCodes.TooManyFiles)
            .WithMessage($"An item import batch cannot contain more than {_limits.MaxFiles} files.")
            .When(x => x.FileMetadataIds is { Count: > 0 });

        RuleFor(x => x.FileMetadataIds)
            .Must(ids => ids is not null && ids.Distinct().Count() == ids.Count)
            .WithErrorCode(ValidationErrorCodes.DuplicateFile)
            .WithMessage("The same file cannot be included twice in the same batch.")
            .When(x => x.FileMetadataIds is { Count: > 0 });

        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) =>
                await ValidateFilesAsync(dbContext, _limits, user, command, context, cancellationToken))
            .When(x => x.FileMetadataIds is { Count: > 0 } ids
                       && ids.Count <= _limits.MaxFiles
                       && ids.Distinct().Count() == ids.Count);
    }

    private static async Task ValidateFilesAsync(
        IApplicationDbContext dbContext,
        ImportBatchOptions limits,
        IUser user,
        CreateItemImportBatchCommand command,
        ValidationContext<CreateItemImportBatchCommand> context,
        CancellationToken cancellationToken)
    {
        var fileMetadataIds = command.FileMetadataIds;

        var files = await dbContext.FileMetadata
            .AsNoTracking()
            .Where(f => fileMetadataIds.Contains(f.Id))
            .Select(f => new { f.Id, f.Status, f.ContentType, f.SizeBytes, f.ETag, f.CreatedById })
            .ToListAsync(cancellationToken);

        var filesById = files.ToDictionary(f => f.Id);

        var missingIds = fileMetadataIds.Where(id => !filesById.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"{missingIds.Count} file(s) do not exist.")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }

        var notCompleted = files.Where(f => f.Status != FileStatus.Completed).Select(f => f.Id).ToList();
        if (notCompleted.Count > 0)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"{notCompleted.Count} file(s) have not completed upload.")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }

        var notOwned = files.Where(f => f.CreatedById != user.Id).Select(f => f.Id).ToList();
        if (notOwned.Count > 0)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"{notOwned.Count} file(s) are not owned by the current user.")
            {
                ErrorCode = ValidationErrorCodes.FileNotOwned
            });
        }

        var duplicateContentIds = files
            .Where(f => !string.IsNullOrWhiteSpace(f.ETag))
            .GroupBy(f => f.ETag!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Select(file => file.Id))
            .ToList();
        if (duplicateContentIds.Count > 0)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                "The same completed file content cannot be included more than once in one aggregate.")
            {
                ErrorCode = ValidationErrorCodes.DuplicateContent
            });
        }

        var disallowedMime = files
            .Where(f => !limits.AllowedContentTypes.Contains(f.ContentType, StringComparer.OrdinalIgnoreCase))
            .Select(f => f.Id)
            .ToList();
        if (disallowedMime.Count > 0)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"{disallowedMime.Count} file(s) have an unsupported content type.")
            {
                ErrorCode = ValidationErrorCodes.UnsupportedFileType
            });
        }

        var tooLarge = files.Where(f => f.SizeBytes > limits.MaxFileSizeBytes).Select(f => f.Id).ToList();
        if (tooLarge.Count > 0)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"{tooLarge.Count} file(s) exceed the maximum file size of {limits.MaxFileSizeBytes} bytes.")
            {
                ErrorCode = ValidationErrorCodes.FileTooLarge
            });
        }

        var totalSize = files.Sum(f => f.SizeBytes);
        if (totalSize > limits.MaxTotalSizeBytes)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"The combined file size ({totalSize} bytes) exceeds the maximum batch size of {limits.MaxTotalSizeBytes} bytes.")
            {
                ErrorCode = ValidationErrorCodes.BatchTooLarge
            });
        }

        var estimatedEncodedSize = ((totalSize + 2) / 3) * 4;
        if (estimatedEncodedSize > limits.MaxEncodedPayloadBytes)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"The estimated encoded provider payload ({estimatedEncodedSize} bytes) exceeds the configured limit of {limits.MaxEncodedPayloadBytes} bytes.")
            {
                ErrorCode = ValidationErrorCodes.EncodedPayloadTooLarge
            });
        }
    }
}
