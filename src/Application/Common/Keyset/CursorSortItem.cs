using System.Text.Json.Serialization;

namespace skestock.Application.Common.Keyset;

public record CursorSortItem
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("direction")]
    public string? Direction { get; init; }
}
