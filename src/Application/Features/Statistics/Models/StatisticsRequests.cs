using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Models;

public static class StatisticsRequests
{
    public class GetClassGoodsReceiptCostsRequest
    {
        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
    }

    public class GetClassDailyConsumptionRequest
    {
        public Guid? ItemId { get; init; }
        public Guid? LocationId { get; init; }
        public Guid? CategoryId { get; init; }
    }

    public class GetDailyConsumptionAveragesRequest
    {
        public Guid? ItemId { get; init; }
        public Guid? LocationId { get; init; }
        public Guid? CategoryId { get; init; }
    }

    public class GetTopPurchasesRequest
    {
        public PurchaseStatisticsScope? Scope { get; init; }
        public Guid? ClassId { get; init; }
        public Guid? CategoryId { get; init; }
        public int? Top { get; init; }
    }

    public class GetItemsPurchaseHistoryRequest
    {
        public List<Guid> ItemIds { get; init; } = [];
    }
}
