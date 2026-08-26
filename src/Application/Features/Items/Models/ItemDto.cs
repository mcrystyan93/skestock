namespace skestock.Application.Features.Items.Models;

public record ItemDto
{
    public int Id { get; init; }
    public string? Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Unit { get; init; } = string.Empty;
    public int MinThreshold { get; init; }
    public bool IsPerishable { get; init; }
    public bool IsActive { get; init; }
    public int CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? CreatedByName { get; init; }
    public string? LastModifiedByName { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset LastModifiedDate { get; init; }
}
