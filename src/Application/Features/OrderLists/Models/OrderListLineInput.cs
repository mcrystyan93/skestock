namespace skestock.Application.Features.OrderLists.Models;

// Shared line payload for create/update commands. ProductName is optional on the way in:
// when ItemId is set and ProductName is blank, the handler snapshots the catalog item's name.
public record OrderListLineInput
{
    public Guid? ItemId { get; init; }
    public string? ProductName { get; init; }
    public decimal Quantity { get; init; }
    public string? Unit { get; init; }
    public string? Notes { get; init; }
}
