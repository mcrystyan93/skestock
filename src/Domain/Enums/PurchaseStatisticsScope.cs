namespace skestock.Domain.Enums;

public enum PurchaseStatisticsScope
{
    Last90Days,  // rolling window across every class
    Last365Days, // rolling window across every class
    Class        // full purchase history of one school class
}
