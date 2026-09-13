using System.ComponentModel.DataAnnotations;

namespace skestock.Application.Common.Models.Options;

/// <summary>
/// Limits applied to aggregate multi-file category and item imports.
/// Bound from configuration (see <c>skestock.Shared.Services.ImportBatchSettings</c>); the property
/// defaults below apply whenever a value is not overridden in configuration.
/// </summary>
public class ImportBatchOptions
{
    /// <summary>Maximum number of files allowed in a single import batch.</summary>
    [Range(1, 100)]
    public int MaxFiles { get; init; } = 10;

    /// <summary>Maximum size, in bytes, of any single file in a batch. Default: 25 MiB.</summary>
    [Range(1, long.MaxValue)]
    public long MaxFileSizeBytes { get; init; } = 25L * 1024 * 1024;

    /// <summary>Maximum combined size, in bytes, of all files in a batch. Default: 100 MiB.</summary>
    [Range(1, long.MaxValue)]
    public long MaxTotalSizeBytes { get; init; } = 100L * 1024 * 1024;

    /// <summary>Maximum number of processing attempts before a batch is marked failed.</summary>
    [Range(1, 20)]
    public int MaxProcessingAttempts { get; init; } = 5;

    /// <summary>
    /// How long a worker owns a processing claim before another delivery may recover it.
    /// Default: 15 minutes. Queue visibility must exceed this value.
    /// </summary>
    [Range(1, 3600)]
    public int ProcessingLeaseSeconds { get; init; } = 15 * 60;

    /// <summary>
    /// Maximum estimated base64 payload size sent to the extraction provider. Default: 140 MiB.
    /// This leaves headroom for the data-URI and JSON envelope above the 100 MiB raw-file limit.
    /// </summary>
    [Range(1, long.MaxValue)]
    public long MaxEncodedPayloadBytes { get; init; } = 140L * 1024 * 1024;

    /// <summary>
    /// Content types (MIME) accepted for import-batch files. Comparison is case-insensitive.
    /// Defaults to the document, spreadsheet, presentation, text, and raster formats supported by
    /// the current OpenAI Responses <c>input_file</c> transport.
    /// </summary>
    public string[] AllowedContentTypes { get; init; } =
    [
        "application/pdf",
        "text/plain",
        "text/csv",
        "application/json",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif",
        "image/heic",
        "image/heif"
    ];
}
