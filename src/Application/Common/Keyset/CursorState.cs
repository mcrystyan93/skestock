using System.Text.Json.Serialization;

namespace skestock.Application.Common.Keyset;

public record CursorState(
    [property: JsonPropertyName("s")]
    List<CursorSortItem> Sort,
    [property: JsonPropertyName("k")]
    Dictionary<string, object?> KeyValues
);
