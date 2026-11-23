using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Expenses.Models;
using skestock.Domain.Entities.Expenses;

namespace skestock.Application.Features.Expenses.Queries.GetAllExpenses;

public class GetAllExpensesHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllExpensesQuery, Result<PaginatedResponse<ExpenseDto>>>
{
    private static readonly IKeysetSortConfiguration<Expense> SortConfiguration = new ExpenseSortConfiguration();
    private static readonly IFilterConfiguration<Expense> FilterConfiguration = new ExpenseFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<ExpenseDto>>> Handle(GetAllExpensesQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<Expense>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<Expense>.Decode(request.Cursor);
        var isSqlServer = TextSearchCollation.IsSqlServer(dbContext.Database);

        var query = FilterQueryBuilder<Expense>.Apply(
            dbContext.Expenses.AsNoTracking().AsQueryable(), request.Filters, FilterConfiguration, isSqlServer);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = isSqlServer
                ? query.Where(e =>
                    (e.SupplierName != null && EF.Functions.Collate(e.SupplierName, TextSearchCollation.AccentInsensitive).Contains(term)) ||
                    (e.DocumentNumber != null && EF.Functions.Collate(e.DocumentNumber, TextSearchCollation.AccentInsensitive).Contains(term)) ||
                    (e.ErrorMessage != null && EF.Functions.Collate(e.ErrorMessage, TextSearchCollation.AccentInsensitive).Contains(term)))
                : query.Where(e =>
                    (e.SupplierName != null && e.SupplierName.Contains(term)) ||
                    (e.DocumentNumber != null && e.DocumentNumber.Contains(term)) ||
                    (e.ErrorMessage != null && e.ErrorMessage.Contains(term)));
        }

        if (cursorState?.KeyValues.Count > 0)
            query = KeysetPredicateBuilder<Expense>.ApplyKeysetPredicate(
                query, effectiveSort, cursorState.KeyValues, SortConfiguration);

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);

        var rows = await OrderByBuilder<Expense>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Take(pageSize + 1)
            .Select(ExpenseDto.Projection)
            .ToListAsync(cancellationToken);

        var hasNextPage = rows.Count > pageSize;
        if (hasNextPage)
            rows.RemoveAt(rows.Count - 1);

        var last = rows.LastOrDefault();
        var cursor = last is null
            ? null
            : new ExpenseCursor(last.Id, last.UploadedAt, last.CreatedDate, last.LastModifiedDate);

        return Result.Ok(new PaginatedResponse<ExpenseDto>
        {
            Data = rows,
            HasNextPage = hasNextPage,
            NextCursor = cursor is not null ? CursorCodec<Expense>.Encode(cursor, effectiveSort) : null,
            Sort =
            [
                ..effectiveSort.Select(s => new PaginationSort
                {
                    Key = char.ToLowerInvariant(s.Key[0]) + s.Key[1..],
                    Value = s.Direction == "desc" ? "descend" : "ascend"
                })
            ]
        });
    }
}
