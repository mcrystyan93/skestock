using skestock.Application.Features.Expenses.Models;

namespace skestock.Application.Features.Expenses.Queries.GetExpenseById;

public class GetExpenseByIdQuery : IRequest<Result<ExpenseDto>>
{
    public Guid Id { get; init; }
}
