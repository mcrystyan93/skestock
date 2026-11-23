using System.Text.Json;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Expenses.Commands.ProcessExpenseDocument;
using skestock.Application.Features.Expenses.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Expenses;
using skestock.Domain.Queues;
using skestock.Shared;

namespace skestock.Application.Features.Expenses.Commands.CreateExpenseFromDocument;

public class CreateExpenseFromDocumentCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<CreateExpenseFromDocumentCommand, Result<ExpenseDto>>
{
    public async ValueTask<Result<ExpenseDto>> Handle(CreateExpenseFromDocumentCommand request, CancellationToken cancellationToken)
    {
        // Expense.UploadedByUserId is a FK to UserProfile.IdentityId, so the raw identity id is stored.
        var identityId = Guard.Against.Null(user.Id, message: "Creating an expense requires an authenticated user.");

        var expense = Expense.CreateFromDocument(request.ClassId, request.FileMetadataId, identityId);
        dbContext.Expenses.Add(expense);

        // The outbox row must be atomic with the expense row.
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(ProcessExpenseDocumentCommand).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new ProcessExpenseDocumentCommand(expense.Id)),
            QueueName = Services.ExpenseQueue,
            UserId = identityId
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(ExpenseDto.Projection.Compile()(expense));
    }
}
