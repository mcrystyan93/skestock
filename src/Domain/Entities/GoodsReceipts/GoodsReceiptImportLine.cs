using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;

namespace skestock.Domain.Entities.GoodsReceipts;

/// <summary>
/// Historical snapshot of a reviewed row saved when its import is confirmed.
/// Stock batch changes must not update these saved values.
/// </summary>
public class GoodsReceiptImportLine : BaseAuditableEntity
{
    public Guid ImportId { get; set; }
    public GoodsReceiptImport Import { get; set; } = null!;

    // Extracted source name, falling back to the resolved catalog name when absent.
    public string RawItemText { get; set; } = null!;

    // Resolved at confirmation, including catalog items created by that confirmation.
    public Guid? MatchedItemId { get; set; }
    public Item? MatchedItem { get; set; }

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}
