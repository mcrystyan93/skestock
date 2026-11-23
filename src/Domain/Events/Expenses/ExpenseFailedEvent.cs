using skestock.Domain.Entities;

namespace skestock.Domain.Events.Expenses;

public class ExpenseFailedEvent(Guid expenseId, Guid classId): BaseEvent
{
    public Guid ExpenseId { get; } = expenseId;
    public Guid ClassId { get; } = classId;
}
