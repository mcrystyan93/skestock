using System.Text.Json;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Expenses.Commands.ProcessExpenseDocument;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Domain.Queues;
using skestock.Shared;

namespace skestock.Application.Features.Expenses.Commands.RetryExpenseProcessing;

public class RetryExpenseProcessingCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<RetryExpenseProcessingCommand, Result>
{
    public async ValueTask<Result> Handle(RetryExpenseProcessingCommand request, CancellationToken cancellationToken)
    {
        var identityId = Guard.Against.Null(user.Id, message: "Retrying an expense requires an authenticated user.");

        var expense = await dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);
        if (expense is null)
            return Result.Fail(new ExpenseErrors.ExpenseNotFound(request.Id));

        if (expense.Status != ExpenseStatus.Failed || expense.FileMetadataId is null)
            return Result.Fail(new ExpenseErrors.ExpenseNotFailed(expense.Id, expense.Status));

        expense.Retry();
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(ProcessExpenseDocumentCommand).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new ProcessExpenseDocumentCommand(expense.Id)),
            QueueName = Services.ExpenseQueue,
            UserId = identityId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
