using System.Text.Json.Nodes;
using Google.GenAI.Types;
using GeminiType = Google.GenAI.Types.Type;

namespace skestock.Infrastructure.AI.Schemas;

/// <summary>
/// Converts a provider-neutral canonical JSON Schema (<see cref="JsonObject"/>) into the
/// strongly-typed <see cref="Schema"/> shape expected by the Google GenAI SDK
/// (<c>GenerateContentConfig.ResponseSchema</c>).
/// </summary>
public static class GeminiSchemaTranslator
{
    public static Schema Translate(JsonObject json)
    {
        var schema = new Schema
        {
            Type = MapType(json["type"]?.GetValue<string>())
        };

        if (json["description"]?.GetValue<string>() is { } description)
            schema.Description = description;

        if (json["properties"] is JsonObject properties)
        {
            var map = new Dictionary<string, Schema>();
            foreach (var (name, value) in properties)
            {
                if (value is JsonObject propObject)
                    map[name] = Translate(propObject);
            }

            schema.Properties = map;
        }

        if (json["required"] is JsonArray required)
        {
            schema.Required = required
                .Where(node => node is not null)
                .Select(node => node!.GetValue<string>())
                .ToList();
        }

        if (json["items"] is JsonObject items)
            schema.Items = Translate(items);

        return schema;
    }

    private static GeminiType MapType(string? type) => type switch
    {
        "string" => GeminiType.String,
        "number" => GeminiType.Number,
        "integer" => GeminiType.Integer,
        "boolean" => GeminiType.Boolean,
        "array" => GeminiType.Array,
        "object" => GeminiType.Object,
        _ => GeminiType.TypeUnspecified
    };
}
