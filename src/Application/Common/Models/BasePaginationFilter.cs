namespace skestock.Application.Common.Models;

public class BasePaginationFilter
{
    public string? SearchTerm { get; init; }
    public int PageSize { get; init; } = PaginationConstants.DEFAULT_PAGE_SIZE;
    /// <summary>Opaque keyset pagination cursor (base64-encoded JSON with sort values and position).</summary>
    public string? Cursor { get; init; }
    public List<PaginationSort> Sort { get; set; } = [];
}

public static class PaginationConstants
{
    public const int DEFAULT_PAGE_SIZE = 50;
}
