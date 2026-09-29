namespace skestock.Application.Features.SupplyLists.Models;

// Shared line payload for create/update. Quantity defaults to 1 and a blank Unit is populated
// from the referenced item.
public record SupplyListLineInput
{
    public Guid ItemId { get; init; }
    public decimal Quantity { get; init; } = 1;
    public string? Unit { get; init; }
    public string? Notes { get; init; }
}
