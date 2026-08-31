using skestock.Domain.Enums;

namespace skestock.Application.Features.Stock.Models;

public static class StockRequests
{
    public class AdjustStockRequest
    {
        public int ClassId { get; init; }
        public int ItemId { get; init; }
        public int LocationId { get; init; }
        public int ActualQuantity { get; init; }
        public AdjustmentReason Reason { get; init; }
    }
}
