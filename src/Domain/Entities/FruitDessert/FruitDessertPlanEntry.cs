using skestock.Domain.Entities.Congregations;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Domain.Entities.FruitDessert;

// One planned working day of a class: which congregation brings fruit and dessert.
public class FruitDessertPlanEntry : BaseAuditableEntity
{
    public Guid SchoolClassId { get; set; }
    public SchoolClass SchoolClass { get; set; } = null!;

    public DateOnly Date { get; set; }

    public Guid? CongregationId { get; set; }
    public Congregation? Congregation { get; set; }

    public bool EmailSent { get; set; }

    public ICollection<FruitDessertMessage> Messages { get; set; } = new List<FruitDessertMessage>();
}
