namespace skestock.Application.Common.Models;

public record PaginatedResponse<T>
{
    public required IEnumerable<T> Data { get; init; }

    /// <summary>Opaque cursor for fetching the next page. Pass as <c>cursor</c> to fetch the next page.</summary>
    public string? NextCursor { get; init; }
    /// <summary>Indicates whether more records exist after this page.</summary>
    public bool HasNextPage { get; init; }
    public required List<PaginationSort> Sort { get; set; } = [];
}
