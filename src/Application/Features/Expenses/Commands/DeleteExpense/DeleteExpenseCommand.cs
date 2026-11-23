using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Security;

namespace skestock.Application.Features.Expenses.Commands.DeleteExpense;

[Authorize]
public class DeleteExpenseCommand(Guid id) : IRequest<Result>, IRequiresOpenClass
{
    Guid? IRequiresOpenClass.ExpenseId => Id;

    public Guid Id { get; init; } = id;
}
