using skestock.Domain.Enums;

namespace skestock.Domain.Entities;

// Materialized purchase totals for one item in one scope (a rolling window across all classes, or
// one class's full history). A purchase is a received stock line (an Order transaction); purchases
// on the same goods receipt count once towards PurchaseCount.
public class ItemPurchaseStatistic : BaseEntity
{
    public PurchaseStatisticsScope Scope { get; set; }

    // Set only when Scope == Class.
    public Guid? ClassId { get; set; }
    public SchoolClass? Class { get; set; }

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public int TotalQuantity { get; set; }

    // Sum of received units multiplied by their batch unit price, in RON.
    public decimal TotalValue { get; set; }

    public int PurchaseCount { get; set; }

    public decimal AverageQuantity { get; set; }

    public decimal AverageUnitPrice { get; set; }

    public DateTimeOffset LastPurchasedAt { get; set; }

    public DateTimeOffset ComputedAt { get; set; }
}
