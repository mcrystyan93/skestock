namespace skestock.Domain.Entities;

public class GoodsReceiptImportLine: BaseAuditableEntity
{
 
    public Guid ImportId { get; set; }
    public GoodsReceiptImport Import { get; set; } = null!;
 
    public string RawItemText { get; set; } = null!; // exactly what the AI read off the file
 
    // Nullable - null until the user (or a fuzzy-match step) confirms which catalog
    // item this line actually refers to. The review screen must not let this line
    // through to confirmation while it's still null.
    public Guid? MatchedItemId { get; set; }
    public Item? MatchedItem { get; set; }
 
    public Guid? LocationId { get; set; } // AI can't know this - user picks it in review
    public Location? Location { get; set; }
 
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}

