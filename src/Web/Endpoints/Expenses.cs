using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.Expenses.Commands.CreateExpense;
using skestock.Application.Features.Expenses.Commands.CreateExpenseFromDocument;
using skestock.Application.Features.Expenses.Commands.DeleteExpense;
using skestock.Application.Features.Expenses.Commands.RetryExpenseProcessing;
using skestock.Application.Features.Expenses.Commands.UpdateExpense;
using skestock.Application.Features.Expenses.Models;
using skestock.Application.Features.Expenses.Queries.GetAllExpenses;
using skestock.Application.Features.Expenses.Queries.GetExpenseById;

namespace skestock.Web.Endpoints;

public class Expenses : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetAllExpenses, "get-all");
        groupBuilder.MapGet(GetExpenseById, "{id:guid}");
        groupBuilder.MapPost(CreateExpense, "");
        groupBuilder.MapPost(CreateExpenseFromDocument, "from-document");
        groupBuilder.MapPut(UpdateExpense, "{id:guid}");
        groupBuilder.MapPost(RetryExpenseProcessing, "{id:guid}/retry");
        groupBuilder.MapDelete(DeleteExpense, "{id:guid}");
    }

    [EndpointSummary("Get all expenses")]
    [EndpointDescription("Retrieves a paginated, filterable list of expenses. Filter by classId, status, source, etc. via the Filters column-filter list.")]
    public static async Task<Results<Ok<PaginatedResponse<ExpenseDto>>, ProblemHttpResult>> GetAllExpenses(
        ISender sender, ExpenseRequests.GetAllExpensesRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllExpensesQuery
        {
            Filters = request.Filters,
            Sort = request.Sort,
            Cursor = request.Cursor,
            SearchTerm = request.SearchTerm,
            PageSize = request.PageSize
        }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Get an expense by id")]
    public static async Task<Results<Ok<ExpenseDto>, ProblemHttpResult>> GetExpenseById(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExpenseByIdQuery { Id = id }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Create an expense from an uploaded document")]
    [EndpointDescription("Creates an expense in the Processing state for an uploaded receipt or invoice and queues it for AI extraction.")]
    public static async Task<Results<Created<ExpenseDto>, ProblemHttpResult>> CreateExpenseFromDocument(
        ISender sender, ExpenseRequests.CreateExpenseFromDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateExpenseFromDocumentCommand
        {
            ClassId = request.ClassId,
            FileMetadataId = request.FileMetadataId
        }, cancellationToken);

        return result.ToCreated(v => $"/api/Expenses/{v.Id}");
    }

    [EndpointSummary("Create an expense manually")]
    public static async Task<Results<Created<ExpenseDto>, ProblemHttpResult>> CreateExpense(
        ISender sender, ExpenseRequests.CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        var f = request.Fields;
        var result = await sender.Send(new CreateExpenseCommand
        {
            ClassId = request.ClassId,
            SupplierName = f.SupplierName,
            DocumentNumber = f.DocumentNumber,
            ExpenseDate = f.ExpenseDate,
            Currency = ExpenseRequests.CurrencyOrDefault(f.Currency),
            TotalAmount = f.TotalAmount,
            VatAmount = f.VatAmount,
            ExpenseCategoryId = f.ExpenseCategoryId
        }, cancellationToken);

        return result.ToCreated(v => $"/api/Expenses/{v.Id}");
    }

    [EndpointSummary("Update an expense")]
    [EndpointDescription("Edits the expense data. Also completes an expense whose extraction failed (it becomes Processed).")]
    public static async Task<Results<Ok<ExpenseDto>, ProblemHttpResult>> UpdateExpense(
        ISender sender, Guid id, ExpenseRequests.ExpenseFieldsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateExpenseCommand
        {
            Id = id,
            SupplierName = request.SupplierName,
            DocumentNumber = request.DocumentNumber,
            ExpenseDate = request.ExpenseDate,
            Currency = ExpenseRequests.CurrencyOrDefault(request.Currency),
            TotalAmount = request.TotalAmount,
            VatAmount = request.VatAmount,
            ExpenseCategoryId = request.ExpenseCategoryId
        }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Retry AI extraction for a failed expense")]
    public static async Task<Results<NoContent, ProblemHttpResult>> RetryExpenseProcessing(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RetryExpenseProcessingCommand(id), cancellationToken);

        return result.ToNoContent();
    }

    [EndpointSummary("Delete an expense")]
    [EndpointDescription("Soft delete: the expense is hidden but kept for history.")]
    public static async Task<Results<NoContent, ProblemHttpResult>> DeleteExpense(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteExpenseCommand(id), cancellationToken);

        return result.ToNoContent();
    }
}
