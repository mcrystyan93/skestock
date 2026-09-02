using skestock.Domain.Enums;

namespace skestock.Application.Features.Stock.Models;

public static class StockRequests
{
    public class AdjustStockRequest
    {
        public Guid ClassId { get; init; }
        public Guid ItemId { get; init; }
        public Guid LocationId { get; init; }
        public int ActualQuantity { get; init; }
        public AdjustmentReason Reason { get; init; }
    }
}
