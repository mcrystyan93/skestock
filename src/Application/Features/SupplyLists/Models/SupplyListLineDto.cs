namespace skestock.Application.Features.SupplyLists.Models;

public record SupplyListLineDto
{
    public Guid Id { get; init; }
    public Guid ItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string? ItemSku { get; init; }
    public string? CategoryName { get; init; }
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
