using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Expenses.Models;

namespace skestock.Application.Features.Expenses.Queries.GetExpenseById;

public class GetExpenseByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetExpenseByIdQuery, Result<ExpenseDto>>
{
    public async ValueTask<Result<ExpenseDto>> Handle(GetExpenseByIdQuery request, CancellationToken cancellationToken)
    {
        var expense = await dbContext.Expenses.AsNoTracking()
            .Where(e => e.Id == request.Id)
            .Select(ExpenseDto.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return expense is null
            ? Result.Fail(new ExpenseErrors.ExpenseNotFound(request.Id))
            : Result.Ok(expense);
    }
}
