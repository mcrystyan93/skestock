using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Expenses.Commands.DeleteExpense;

// Soft delete: the row stays for history; the query filter hides it.
public class DeleteExpenseCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteExpenseCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);
        if (expense is null)
            return Result.Fail(new ExpenseErrors.ExpenseNotFound(request.Id));

        expense.DeletedDate = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
