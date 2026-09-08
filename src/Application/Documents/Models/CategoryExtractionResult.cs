using System.Text.Json.Serialization;

namespace skestock.Application.Documents.Models;

public sealed class CategoryExtractionResult
{
    [JsonPropertyName("categories")]
    public List<ExtractedCategory> Categories { get; set; } = [];
}

public sealed class ExtractedCategory
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
