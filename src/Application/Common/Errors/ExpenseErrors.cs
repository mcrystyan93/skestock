namespace skestock.Application.Common.Errors;
using Microsoft.AspNetCore.Http;
using skestock.Domain.Enums;

public static class ExpenseErrors
{
    public sealed class ExpenseNotFound : Error
    {
        public const string ErrorCode = "expenses.not_found";

        public ExpenseNotFound(Guid expenseId) : base($"Expense with id '{expenseId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Expense not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["expenseId"] = expenseId });
        }
    }

    public sealed class ExpenseNotFailed : Error
    {
        public const string ErrorCode = "expenses.not_failed";

        public ExpenseNotFailed(Guid expenseId, ExpenseStatus status)
            : base($"Expense with id '{expenseId}' is '{status}'. Only failed expenses with a document can be retried.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Expense is not failed");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["expenseId"] = expenseId,
                ["status"] = status.ToString()
            });
        }
    }

    public sealed class ExpenseBeingProcessed : Error
    {
        public const string ErrorCode = "expenses.being_processed";

        public ExpenseBeingProcessed(Guid expenseId)
            : base($"Expense with id '{expenseId}' is still being processed and cannot be edited yet.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Expense is being processed");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["expenseId"] = expenseId });
        }
    }
}
