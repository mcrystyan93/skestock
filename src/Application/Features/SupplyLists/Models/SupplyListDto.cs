namespace skestock.Application.Features.SupplyLists.Models;

public record SupplyListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Note { get; init; }
    public string Frequency { get; init; } = string.Empty;
    public int? IntervalWeeks { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<SupplyListLineDto> Lines { get; init; } = [];
    public string? CreatedByName { get; init; }
    public string? LastModifiedByName { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset LastModifiedDate { get; init; }
}
