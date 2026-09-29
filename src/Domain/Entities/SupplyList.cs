using skestock.Domain.Enums;

namespace skestock.Domain.Entities;

public class SupplyList : BaseAuditableEntity, IKeysetEntity
{
    public string Name { get; set; } = null!;
    public string? Note { get; set; }
    public SupplyListFrequency Frequency { get; set; }

    // Only set when Frequency is EveryXWeeks.
    public int? IntervalWeeks { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<SupplyListLine> Lines { get; set; } = new List<SupplyListLine>();
}
