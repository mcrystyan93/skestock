using skestock.Domain.Enums;

namespace skestock.Domain.Entities.FruitDessert;

// At most one active template per Type (filtered unique index).
public class MessageTemplate : BaseAuditableEntity
{
    public FruitDessertMessageType Type { get; set; }
    public string Name { get; set; } = null!;
    public string Body { get; set; } = null!;
    public bool IsActive { get; set; }
}
