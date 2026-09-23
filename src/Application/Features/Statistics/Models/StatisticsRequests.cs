namespace skestock.Application.Features.Statistics.Models;

public static class StatisticsRequests
{
    public class GetClassGoodsReceiptCostsRequest
    {
        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
    }
}
