namespace skestock.Application.Features.Statistics.Models;

public static class StatisticsRequests
{
    public class GetClassStockByCategoryRequest
    {
        public Guid? LocationId { get; init; }
    }

    public class GetClassGoodsReceiptCostsRequest
    {
        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
    }
}
