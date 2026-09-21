namespace skestock.Application.Common.Models;

// A generated file returned to the caller (bytes + name + MIME type) so endpoints
// can stream it without any layer-specific file abstraction.
public record FileExportResult
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
}
