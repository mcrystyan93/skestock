using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetClassDailyConsumption;

public class GetClassDailyConsumptionHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<GetClassDailyConsumptionQuery, Result<ClassDailyConsumptionDto>>
{
    public async ValueTask<Result<ClassDailyConsumptionDto>> Handle(
        GetClassDailyConsumptionQuery request,
        CancellationToken cancellationToken)
    {
        var schoolClass = await dbContext.SchoolClasses
            .AsNoTracking()
            .Where(c => c.Id == request.ClassId)
            .Select(c => new { c.Id, c.EndDate })
            .SingleOrDefaultAsync(cancellationToken);

        if (schoolClass is null)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.ClassId));

        var firstTransactionAt = await dbContext.StockTransactions
            .AsNoTracking()
            .Where(t => t.ClassId == request.ClassId)
            .MinAsync(t => (DateTimeOffset?)t.CreatedAt, cancellationToken);

        if (firstTransactionAt is null)
            return Result.Ok(new ClassDailyConsumptionDto { ClassId = request.ClassId });

        var totalsByDate = await dbContext.DailyItemConsumptions
            .AsNoTracking()
            .Where(c => c.ClassId == request.ClassId)
            .GroupBy(c => c.Date)
            .Select(g => new
            {
                Date = g.Key,
                Quantity = g.Sum(c => c.Quantity),
                Value = g.Sum(c => c.TotalValue)
            })
            .ToDictionaryAsync(t => t.Date, cancellationToken);

        var fromDate = LocalCalendarDay.GetDate(firstTransactionAt.Value, DailyConsumptionCalendar.TimeZone);
        var today = DailyConsumptionCalendar.Today(timeProvider);
        var toDate = schoolClass.EndDate < today ? schoolClass.EndDate : today;

        // Consumption recorded after the class end (or before its first transaction) stays visible.
        if (totalsByDate.Count > 0)
        {
            var lastDate = totalsByDate.Keys.Max();
            var firstDate = totalsByDate.Keys.Min();
            if (lastDate > toDate) toDate = lastDate;
            if (firstDate < fromDate) fromDate = firstDate;
        }

        if (toDate < fromDate)
            toDate = fromDate;

        var points = new List<DailyConsumptionPointDto>(toDate.DayNumber - fromDate.DayNumber + 1);
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            totalsByDate.TryGetValue(date, out var total);
            points.Add(new DailyConsumptionPointDto
            {
                Date = date,
                Quantity = total?.Quantity ?? 0,
                Value = total?.Value ?? 0m
            });
        }

        var totalQuantity = points.Sum(p => p.Quantity);
        var totalValue = points.Sum(p => p.Value);

        return Result.Ok(new ClassDailyConsumptionDto
        {
            ClassId = request.ClassId,
            FromDate = fromDate,
            ToDate = toDate,
            Points = points,
            TotalQuantity = totalQuantity,
            TotalValue = totalValue,
            AverageQuantity = Math.Round((decimal)totalQuantity / points.Count, 2),
            AverageValue = Math.Round(totalValue / points.Count, 2)
        });
    }
}
