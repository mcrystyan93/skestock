using skestock.Domain.Enums;

namespace skestock.Domain.Entities.FruitDessert;

// Append-only history of a message dispatch; Body and RecipientPhone are snapshots.
public class FruitDessertMessage : BaseAuditableEntity
{
    public Guid EntryId { get; set; }
    public FruitDessertPlanEntry Entry { get; set; } = null!;

    public FruitDessertMessageType Type { get; set; }
    public FruitDessertMessageStatus Status { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public string RecipientPhone { get; set; } = null!;
    public string Body { get; set; } = null!;

    public Guid TemplateId { get; set; }
    public MessageTemplate Template { get; set; } = null!;
}
