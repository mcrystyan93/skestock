namespace skestock.Domain.Entities;

// Materialized daily consumption total for one item in one class/location. Consumption is the
// gross sum of negative, non-transfer stock transactions within a local calendar day.
public class DailyItemConsumption : BaseEntity
{
    public DateOnly Date { get; set; }

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public Guid ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;

    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;

    // Consumed units as a positive number.
    public int Quantity { get; set; }

    // Consumed value in RON: sum of consumed units multiplied by their batch unit price.
    public decimal TotalValue { get; set; }

    public DateTimeOffset ComputedAt { get; set; }
}
