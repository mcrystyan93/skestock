using System.Text.Json;
using System.Text.Json.Serialization;
using skestock.Shared.JsonConverters;

namespace skestock.Application.Features.Expenses.Models;

public class ExpenseExtractionResult
{
    [JsonPropertyName("documentType")]
    public string? DocumentType { get; set; } // "receipt" | "invoice"
    [JsonPropertyName("supplierName")]
    public string? SupplierName { get; set; }
    [JsonPropertyName("documentNumber")]
    public string? DocumentNumber { get; set; }
    [JsonPropertyName("expenseDate")]
    [JsonConverter(typeof(FlexibleDateOnlyJsonConverter))]
    public DateOnly? ExpenseDate { get; set; }
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
    [JsonPropertyName("totalAmount")]
    public decimal? TotalAmount { get; set; }
    [JsonPropertyName("vatAmount")]
    public decimal? VatAmount { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this);
}
