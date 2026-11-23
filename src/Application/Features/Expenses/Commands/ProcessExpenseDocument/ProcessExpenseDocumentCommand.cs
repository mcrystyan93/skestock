namespace skestock.Application.Features.Expenses.Commands.ProcessExpenseDocument;

public class ProcessExpenseDocumentCommand(Guid expenseId) : IRequest<Result>
{
    public Guid ExpenseId { get; init; } = expenseId;
}
