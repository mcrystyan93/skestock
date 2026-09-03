using System.Text.Json;

namespace skestock.Infrastructure.AI.Models;

public class NutrientExtractResponse
{
    public NutrientExtractOutput? Output { get; set; }
    public string? ErrorMessage { get; set; } // present on failed calls per Nutrient's docs
}

public class NutrientExtractOutput
{
    public JsonElement Data { get; set; }       // shaped to your schema
    public JsonElement? Metadata { get; set; }  // citations / confidence, if you need them later
}
