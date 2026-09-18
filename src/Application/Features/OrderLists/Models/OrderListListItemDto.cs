namespace skestock.Application.Features.OrderLists.Models;

public record OrderListListItemDto
{
    public Guid Id { get; init; }
    public Guid ClassId { get; init; }
    public string? ClassName { get; init; }
    public string? Name { get; init; }
    public string Status { get; init; } = string.Empty;
    public int LineCount { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public string? CreatedByName { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset LastModifiedDate { get; init; }
}
