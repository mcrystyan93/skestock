using Microsoft.Extensions.Logging;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Interfaces;
using skestock.Application.Documents.Interfaces;
using skestock.Application.Features.Expenses.Models;
using skestock.Application.Queues;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Expenses.Commands.ProcessExpenseDocument;

public class ProcessExpenseDocumentCommandHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    IStockDocumentExtractionService documentExtractionService,
    MessageDeliveryContext deliveryContext,
    ILogger<ProcessExpenseDocumentCommandHandler> logger) : IRequestHandler<ProcessExpenseDocumentCommand, Result>
{
    public async ValueTask<Result> Handle(ProcessExpenseDocumentCommand request, CancellationToken cancellationToken)
    {
        var expense = await dbContext.Expenses
            .Include(x => x.FileMetadata)
            .FirstOrDefaultAsync(x => x.Id == request.ExpenseId, cancellationToken);

        if (expense is null)
        {
            logger.LogError("Expense not found: {ExpenseId}", request.ExpenseId);
            return Result.Fail(new ExpenseErrors.ExpenseNotFound(request.ExpenseId));
        }

        // Idempotency at the handler level: a replayed message, or an expense the user already
        // completed by hand, must not be overwritten by a late extraction.
        if (expense.Status != ExpenseStatus.Processing || expense.FileMetadata is null)
        {
            logger.LogInformation("Expense already handled: {ExpenseId}, Status: {Status}", expense.Id, expense.Status);
            return Result.Ok();
        }

        try
        {
            await using var fileStream = await blobStorageService.DownloadAsync(
                new(expense.FileMetadata.BlobPath, expense.FileMetadata.BlobContainer), cancellationToken);

            var extraction = await documentExtractionService.ExtractAsync<ExpenseExtractionResult>(
                fileStream, expense.FileMetadata.ContentType, cancellationToken);

            var source = string.Equals(extraction.DocumentType, "invoice", StringComparison.OrdinalIgnoreCase)
                ? ExpenseSource.Invoice
                : ExpenseSource.Receipt;

            expense.ApplyExtractionResult(
                extraction.ToJson(), source, extraction.SupplierName, extraction.DocumentNumber,
                extraction.ExpenseDate, extraction.Currency, extraction.TotalAmount, extraction.VatAmount);
        }
        catch (UnprocessableDocumentException ex)
        {
            // Permanent failure: retrying the same input would fail identically.
            // ex.Message can hold the provider's raw error body: keep it in the log, never in the user-visible
            // (and 2000-char-limited) ErrorMessage.
            logger.LogError(ex, "Unprocessable expense document: {ExpenseId}", expense.Id);
            expense.MarkAsFailed("Documentul nu a putut fi citit. Completează datele manual sau reîncearcă.");
        }
        catch (TransientExtractionException ex) when (deliveryContext.IsFinalAttempt)
        {
            // Retries are exhausted: the message is about to be poisoned, so surface the failure to the user
            // instead of leaving the expense "In procesare" forever.
            logger.LogError(ex, "Expense extraction kept failing, giving up: {ExpenseId}", expense.Id);
            expense.MarkAsFailed("Serviciul de citire a documentelor nu a răspuns. Reîncearcă sau completează datele manual.");
        }
        // Earlier transient failures propagate so the queue processor retries.

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
