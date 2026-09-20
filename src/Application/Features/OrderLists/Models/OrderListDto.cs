namespace skestock.Application.Features.OrderLists.Models;

public record OrderListDto
{
    public Guid Id { get; init; }
    public Guid ClassId { get; init; }
    public string? ClassName { get; init; }
    public string? Name { get; init; }
    public string? Note { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset? SubmittedAt { get; init; }
    public IReadOnlyList<OrderListLineDto> Lines { get; init; } = [];
    public string? CreatedByName { get; init; }
    public string? LastModifiedByName { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset LastModifiedDate { get; init; }
}
