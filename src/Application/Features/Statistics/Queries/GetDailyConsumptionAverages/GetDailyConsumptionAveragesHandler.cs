using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetDailyConsumptionAverages;

public class GetDailyConsumptionAveragesHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<GetDailyConsumptionAveragesQuery, Result<DailyConsumptionAveragesDto>>
{
    public async ValueTask<Result<DailyConsumptionAveragesDto>> Handle(
        GetDailyConsumptionAveragesQuery request,
        CancellationToken cancellationToken)
    {
        // Windows end yesterday: today's row is provisional until the day is over.
        var toDate = DailyConsumptionCalendar.Today(timeProvider).AddDays(-1);
        var fromDate = toDate.AddDays(-29);

        var consumptions = dbContext.DailyItemConsumptions
            .AsNoTracking()
            .Where(c => c.Date >= fromDate && c.Date <= toDate);

        if (request.ItemId is { } itemId)
            consumptions = consumptions.Where(c => c.ItemId == itemId);

        if (request.LocationId is { } locationId)
            consumptions = consumptions.Where(c => c.LocationId == locationId);

        if (request.CategoryId is { } categoryId)
            consumptions = consumptions.Where(c => c.Item.CategoryId == categoryId);

        var totalsByDate = await consumptions
            .GroupBy(c => c.Date)
            .Select(g => new
            {
                Date = g.Key,
                Quantity = g.Sum(c => c.Quantity),
                Value = g.Sum(c => c.TotalValue)
            })
            .ToListAsync(cancellationToken);

        DailyConsumptionAverageDto Window(int days)
        {
            var windowStart = toDate.AddDays(-(days - 1));
            var inWindow = totalsByDate.Where(t => t.Date >= windowStart).ToList();
            var quantity = inWindow.Sum(t => t.Quantity);
            var value = inWindow.Sum(t => t.Value);

            return new DailyConsumptionAverageDto
            {
                FromDate = windowStart,
                ToDate = toDate,
                Days = days,
                TotalQuantity = quantity,
                TotalValue = value,
                AverageQuantity = Math.Round((decimal)quantity / days, 2),
                AverageValue = Math.Round(value / days, 2)
            };
        }

        return Result.Ok(new DailyConsumptionAveragesDto
        {
            Last7Days = Window(7),
            Last30Days = Window(30)
        });
    }
}
