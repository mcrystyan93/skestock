using skestock.Application.Common.Filtering;
using skestock.Domain.Enums;
using System.Text.Json.Serialization;

namespace skestock.Application.Features.Stock.Models;

public static class StockRequests
{
    public class GetClassLocationStockRequest
    {
        public string? SearchTerm { get; init; }
        public List<ColumnFilter> Filters { get; init; } = [];
        public bool IncludeHidden { get; init; }
        public bool LowStockOnly { get; init; }
        public bool ExpiredOnly { get; init; }
    }

    public class AdjustStockRequest
    {
        public Guid ClassId { get; init; }
        public Guid ItemId { get; init; }
        public Guid LocationId { get; init; }
        public int ActualQuantity { get; init; }
        public AdjustmentReason Reason { get; init; }
    }

    public class RemoveExpiredStockRequest
    {
        public Guid ClassId { get; init; }
        public Guid ItemId { get; init; }
        public Guid LocationId { get; init; }
    }

    public class MoveStockRequest
    {
        [JsonPropertyName("classId")] public Guid ClassId { get; init; }

        [JsonPropertyName("itemId")] public Guid ItemId { get; init; }

        [JsonPropertyName("sourceLocationId")] public Guid SourceLocationId { get; init; }

        [JsonPropertyName("destinationLocationId")]
        public Guid DestinationLocationId { get; init; }

        [JsonPropertyName("quantity")] public int Quantity { get; init; }
    }

    public class SetClassItemStockVisibilityRequest
    {
        public bool HideWhenZeroStock { get; init; }
    }
}
