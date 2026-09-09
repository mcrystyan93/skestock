using System.Text.Json.Serialization;

namespace skestock.Application.Documents.Models;

public sealed class ItemExtractionResult
{
    [JsonPropertyName("items")] public List<ExtractedItem> Items { get; set; } = [];
}

public sealed class ExtractedItem
{
    [JsonPropertyName("sku")] public string? Sku { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("categoryName")] public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("unit")] public string? Unit { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("isPerishable")] public bool IsPerishable { get; set; }
}
