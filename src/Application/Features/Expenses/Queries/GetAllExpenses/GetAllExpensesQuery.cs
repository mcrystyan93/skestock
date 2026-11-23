using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.Expenses.Models;

namespace skestock.Application.Features.Expenses.Queries.GetAllExpenses;

// Not cached: the list changes on every status event and is already served by keyset indexes.
public class GetAllExpensesQuery : BasePaginationFilter, IRequest<Result<PaginatedResponse<ExpenseDto>>>
{
    public List<ColumnFilter> Filters { get; init; } = [];
}
