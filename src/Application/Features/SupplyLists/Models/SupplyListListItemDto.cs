namespace skestock.Application.Features.SupplyLists.Models;

public record SupplyListListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public int? IntervalWeeks { get; init; }
    public bool IsActive { get; init; }
    public int LineCount { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset LastModifiedDate { get; init; }
}
