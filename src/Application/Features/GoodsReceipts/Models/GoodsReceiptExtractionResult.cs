using System.Text.Json;
using System.Text.Json.Serialization;
using skestock.Shared.JsonConverters;

namespace skestock.Application.Features.GoodsReceipts.Models;

public class GoodsReceiptExtractionResult
{
    [JsonPropertyName("supplierReference")]
    public string? SupplierReference { get; set; }
    [JsonPropertyName("receivedAt")]
    [JsonConverter(typeof(FlexibleDateOnlyJsonConverter))]
    public DateOnly? ReceivedAt { get; set; }
    [JsonPropertyName("lineItems")]
    public List<GoodsReceiptLineItem> LineItems { get; set; } = new();
    
    public string ToJson() => JsonSerializer.Serialize(this);
}
public class GoodsReceiptLineItem
{
    [JsonPropertyName("productCode")]
    public string? ProductCode { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }
    [JsonPropertyName("unitPrice")]
    public decimal? UnitPrice { get; set; }
    [JsonPropertyName("isPerishable")]
    public bool IsPerishable { get; set; }
}
