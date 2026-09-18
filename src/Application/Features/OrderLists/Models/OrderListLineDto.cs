namespace skestock.Application.Features.OrderLists.Models;

public record OrderListLineDto
{
    public Guid Id { get; init; }
    public Guid? ItemId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string? Unit { get; init; }
    public string? Notes { get; init; }
}
