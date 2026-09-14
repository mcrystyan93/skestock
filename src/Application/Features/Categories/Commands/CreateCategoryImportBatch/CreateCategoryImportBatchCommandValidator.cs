using FluentValidation.Results;
using Microsoft.Extensions.Options;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models.Options;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Commands.CreateCategoryImportBatch;

public class CreateCategoryImportBatchCommandValidator : AbstractValidator<CreateCategoryImportBatchCommand>
{
    private readonly ImportBatchOptions _limits;

    public CreateCategoryImportBatchCommandValidator(
        IApplicationDbContext dbContext, IOptions<ImportBatchOptions> importBatchOptions, IUser user)
    {
        _limits = importBatchOptions.Value;

        RuleFor(command => command.FileMetadataIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .Must(ids => ids is { Count: >= 1 })
            .WithErrorCode(ValidationErrorCodes.TooFewFiles)
            .WithMessage("At least one file is required for a category import batch.");

        RuleFor(command => command.FileMetadataIds)
            .Must(ids => ids is not null && ids.Count <= _limits.MaxFiles)
            .WithErrorCode(ValidationErrorCodes.TooManyFiles)
            .WithMessage($"A category import batch cannot contain more than {_limits.MaxFiles} files.")
            .When(command => command.FileMetadataIds is { Count: > 0 });

        RuleFor(command => command.FileMetadataIds)
            .Must(ids => ids is not null && ids.Distinct().Count() == ids.Count)
            .WithErrorCode(ValidationErrorCodes.DuplicateFile)
            .WithMessage("The same file cannot be included twice in the same batch.")
            .When(command => command.FileMetadataIds is { Count: > 0 });

        RuleFor(command => command)
            .CustomAsync(async (command, context, cancellationToken) =>
                await ValidateFilesAsync(dbContext, _limits, user, command, context, cancellationToken))
            .When(command => command.FileMetadataIds is { Count: > 0 } ids
                             && ids.Count <= _limits.MaxFiles
                             && ids.Distinct().Count() == ids.Count);
    }

    private static async Task ValidateFilesAsync(
        IApplicationDbContext dbContext,
        ImportBatchOptions limits,
        IUser user,
        CreateCategoryImportBatchCommand command,
        ValidationContext<CreateCategoryImportBatchCommand> context,
        CancellationToken cancellationToken)
    {
        var fileMetadataIds = command.FileMetadataIds;
        var files = await dbContext.FileMetadata
            .AsNoTracking()
            .Where(file => fileMetadataIds.Contains(file.Id))
            .Select(file => new
            {
                file.Id,
                file.Status,
                file.ContentType,
                file.SizeBytes,
                file.ETag,
                file.CreatedById
            })
            .ToListAsync(cancellationToken);

        var filesById = files.ToDictionary(file => file.Id);
        var missingIds = fileMetadataIds.Where(id => !filesById.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"{missingIds.Count} file(s) do not exist.")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }

        var notCompleted = files.Where(file => file.Status != FileStatus.Completed).Select(file => file.Id).ToList();
        if (notCompleted.Count > 0)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"{notCompleted.Count} file(s) have not completed upload.")
            {
                ErrorCode = ValidationErrorCodes.InvalidReference
            });
        }

        var notOwned = files.Where(file => file.CreatedById != user.Id).Select(file => file.Id).ToList();
        if (notOwned.Count > 0)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"{notOwned.Count} file(s) are not owned by the current user.")
            {
                ErrorCode = ValidationErrorCodes.FileNotOwned
            });
        }

        var duplicateContentIds = files
            .Where(file => !string.IsNullOrWhiteSpace(file.ETag))
            .GroupBy(file => file.ETag!, StringComparer.Ordinal)
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
            .Where(file => !limits.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            .Select(file => file.Id)
            .ToList();
        if (disallowedMime.Count > 0)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"{disallowedMime.Count} file(s) have an unsupported content type.")
            {
                ErrorCode = ValidationErrorCodes.UnsupportedFileType
            });
        }

        var tooLarge = files.Where(file => file.SizeBytes > limits.MaxFileSizeBytes).Select(file => file.Id).ToList();
        if (tooLarge.Count > 0)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"{tooLarge.Count} file(s) exceed the maximum file size of {limits.MaxFileSizeBytes} bytes.")
            {
                ErrorCode = ValidationErrorCodes.FileTooLarge
            });
        }

        var totalSize = files.Sum(file => file.SizeBytes);
        if (totalSize > limits.MaxTotalSizeBytes)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"The combined file size ({totalSize} bytes) exceeds the configured maximum batch size.")
            {
                ErrorCode = ValidationErrorCodes.BatchTooLarge
            });
        }

        var estimatedEncodedSize = ((totalSize + 2) / 3) * 4;
        if (estimatedEncodedSize > limits.MaxEncodedPayloadBytes)
        {
            context.AddFailure(new ValidationFailure(nameof(command.FileMetadataIds),
                $"The estimated encoded provider payload ({estimatedEncodedSize} bytes) exceeds the configured limit.")
            {
                ErrorCode = ValidationErrorCodes.EncodedPayloadTooLarge
            });
        }
    }
}
