using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Queries.GetClassEvolution;

public class GetClassEvolutionQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetClassEvolutionQuery, Result<ClassEvolutionDto>>
{
    public async ValueTask<Result<ClassEvolutionDto>> Handle(
        GetClassEvolutionQuery request,
        CancellationToken cancellationToken)
    {
        var classId = request.ClassId;

        if (!await dbContext.SchoolClasses.AsNoTracking().AnyAsync(c => c.Id == classId, cancellationToken))
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(classId));

        // Sequential: a DbContext does not allow overlapping operations.
        var orders = (await dbContext.GoodsReceipts
                .AsNoTracking()
                .Where(r => r.ClassId == classId)
                .GroupBy(r => r.ReceivedAt.Date)
                .Select(g => new { Day = g.Key, Total = g.Sum(r => r.TotalAmount) })
                .ToListAsync(cancellationToken))
            .Select(x => (Day: DateOnly.FromDateTime(x.Day), x.Total))
            .ToList();

        var donations = (await dbContext.StockTransactions
                .AsNoTracking()
                .Where(t => t.ClassId == classId && t.Type == StockTransactionType.Donation && t.QuantityChange > 0)
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new
                {
                    Day = g.Key,
                    Total = g.Sum(t => t.QuantityChange * (t.Batch != null ? t.Batch.UnitPrice : 0m))
                })
                .ToListAsync(cancellationToken))
            .Select(x => (Day: DateOnly.FromDateTime(x.Day), x.Total))
            .ToList();

        // Undated expenses cannot be placed on the timeline.
        var expenses = (await dbContext.Expenses
                .AsNoTracking()
                .Where(e => e.SchoolClassId == classId
                            && e.Status == ExpenseStatus.Processed
                            && e.ExpenseDate != null)
                .GroupBy(e => e.ExpenseDate!.Value)
                .Select(g => new { Day = g.Key, Total = g.Sum(e => e.TotalAmount ?? 0m) })
                .ToListAsync(cancellationToken))
            .Select(x => (x.Day, x.Total))
            .ToList();

        var weekTotals = (IEnumerable<(DateOnly Day, decimal Total)> entries) => entries
            .GroupBy(e => WeekStart(e.Day))
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Total));
        var ordersByWeek = weekTotals(orders);
        var donationsByWeek = weekTotals(donations);
        var expensesByWeek = weekTotals(expenses);

        var weeks = ordersByWeek.Keys.Concat(donationsByWeek.Keys).Concat(expensesByWeek.Keys).ToList();
        if (weeks.Count == 0)
            return Result.Ok(new ClassEvolutionDto());

        // First to last order, donation or expense; weeks in between are zero-filled.
        var first = weeks.Min();
        // Index-based so a week near DateOnly.MaxValue cannot overflow AddDays after the last point.
        var weekCount = (weeks.Max().DayNumber - first.DayNumber) / 7 + 1;
        var points = Enumerable.Range(0, weekCount)
            .Select(i => first.AddDays(i * 7))
            .Select(week => new ClassEvolutionPointDto
            {
                WeekStart = week,
                Orders = ordersByWeek.GetValueOrDefault(week),
                Donations = Math.Round(donationsByWeek.GetValueOrDefault(week), 2),
                Expenses = expensesByWeek.GetValueOrDefault(week)
            })
            .ToList();

        return Result.Ok(new ClassEvolutionDto { Points = points });
    }

    // Weeks start on Monday.
    private static DateOnly WeekStart(DateOnly day) =>
        day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
}
