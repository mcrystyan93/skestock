using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Queries.GetClassSummary;

public class GetClassSummaryQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetClassSummaryQuery, Result<ClassSummaryDto>>
{
    public async ValueTask<Result<ClassSummaryDto>> Handle(
        GetClassSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var classId = request.ClassId;

        if (!await dbContext.SchoolClasses.AsNoTracking().AnyAsync(c => c.Id == classId, cancellationToken))
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(classId));

        // Sequential: a DbContext does not allow overlapping operations.
        var orders = await dbContext.GoodsReceipts
            .AsNoTracking()
            .Where(r => r.ClassId == classId)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Total = g.Sum(r => r.TotalAmount) })
            .FirstOrDefaultAsync(cancellationToken);

        // Each donation transaction is one donated batch line.
        var donationQuery = dbContext.StockTransactions
            .AsNoTracking()
            .Where(t => t.ClassId == classId && t.Type == StockTransactionType.Donation && t.QuantityChange > 0);
        var donationCount = await donationQuery.CountAsync(cancellationToken);
        var donations = await donationQuery
            .SumAsync(t => (decimal?)(t.QuantityChange * (t.Batch != null ? t.Batch.UnitPrice : 0m)), cancellationToken);

        var expenses = await dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.SchoolClassId == classId && e.Status == ExpenseStatus.Processed)
            .SumAsync(e => e.TotalAmount, cancellationToken);

        var stockValue = await dbContext.StockBatches
            .AsNoTracking()
            .Where(b => b.ReceivedClassId == classId)
            .SumAsync(b => (decimal?)(b.Quantity * b.UnitPrice), cancellationToken);

        return Result.Ok(new ClassSummaryDto
        {
            OrderCount = orders?.Count ?? 0,
            TotalOrders = orders?.Total ?? 0m,
            DonationCount = donationCount,
            TotalDonations = Math.Round(donations ?? 0m, 2),
            TotalExpenses = expenses ?? 0m,
            CurrentStockValue = Math.Round(stockValue ?? 0m, 2)
        });
    }
}
