using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Application.Features.Statistics;

namespace skestock.Application.Features.Statistics.Commands.MaterializeDailyConsumption;

public sealed class MaterializeDailyConsumptionCommandHandler(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider)
    : IRequestHandler<MaterializeDailyConsumptionCommand, Result<int>>
{
    public async ValueTask<Result<int>> Handle(
        MaterializeDailyConsumptionCommand request,
        CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
        var computedAt = timeProvider.GetUtcNow();

        var rows = new List<DailyItemConsumption>();
        for (var date = request.FromDate; date <= request.ToDate; date = date.AddDays(1))
        {
            rows.AddRange(await AggregateDayAsync(date, timeZone, computedAt, cancellationToken));
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(
            async strategyCancellationToken =>
            {
                dbContext.DailyItemConsumptions.Local.Clear();

                await using var transaction =
                    await dbContext.Database.BeginTransactionAsync(strategyCancellationToken);

                await dbContext.DailyItemConsumptions
                    .Where(c => c.Date >= request.FromDate && c.Date <= request.ToDate)
                    .ExecuteDeleteAsync(strategyCancellationToken);

                dbContext.DailyItemConsumptions.AddRange(rows.Select(Clone));
                await dbContext.SaveChangesAsync(strategyCancellationToken);
                await transaction.CommitAsync(strategyCancellationToken);
            },
            cancellationToken);

        return Result.Ok(rows.Count);
    }

    private async Task<List<DailyItemConsumption>> AggregateDayAsync(
        DateOnly date,
        TimeZoneInfo timeZone,
        DateTimeOffset computedAt,
        CancellationToken cancellationToken)
    {
        var startUtc = LocalCalendarDay.GetStartUtc(date, timeZone);
        var endUtc = LocalCalendarDay.GetStartUtc(date.AddDays(1), timeZone);

        var totals = await dbContext.StockTransactions
            .AsNoTracking()
            .Where(t => t.CreatedAt >= startUtc
                && t.CreatedAt < endUtc
                && t.QuantityChange < 0
                && t.Type != StockTransactionType.Transfer)
            .GroupBy(t => new { t.ItemId, t.ClassId, t.LocationId })
            .Select(g => new
            {
                g.Key.ItemId,
                g.Key.ClassId,
                g.Key.LocationId,
                Quantity = g.Sum(t => -t.QuantityChange),
                TotalValue = g.Sum(t =>
                    -t.QuantityChange * (t.Batch != null ? t.Batch.UnitPrice : 0m))
            })
            .ToListAsync(cancellationToken);

        return totals
            .Select(total => new DailyItemConsumption
            {
                Date = date,
                ItemId = total.ItemId,
                ClassId = total.ClassId,
                LocationId = total.LocationId,
                Quantity = total.Quantity,
                TotalValue = total.TotalValue,
                ComputedAt = computedAt
            })
            .ToList();
    }

    // A fresh instance per strategy attempt, so a retried execution never re-adds tracked entities.
    private static DailyItemConsumption Clone(DailyItemConsumption source) => new()
    {
        Date = source.Date,
        ItemId = source.ItemId,
        ClassId = source.ClassId,
        LocationId = source.LocationId,
        Quantity = source.Quantity,
        TotalValue = source.TotalValue,
        ComputedAt = source.ComputedAt
    };
}
