namespace skestock.Domain.Entities.Expenses;

public class ExpenseCategory : BaseAuditableEntity
{
    public string Name { get; set; } = null!;
}
