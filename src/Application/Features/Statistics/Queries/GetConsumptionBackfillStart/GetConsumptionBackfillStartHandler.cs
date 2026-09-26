using skestock.Application.Common.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Queries.GetConsumptionBackfillStart;

public sealed class GetConsumptionBackfillStartHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetConsumptionBackfillStartQuery, Result<DateOnly?>>
{
    public async ValueTask<Result<DateOnly?>> Handle(
        GetConsumptionBackfillStartQuery request,
        CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);

        // Same predicate as MaterializeDailyConsumptionCommandHandler.
        var earliestConsumptionAt = await dbContext.StockTransactions
            .AsNoTracking()
            .Where(t => t.QuantityChange < 0 && t.Type != StockTransactionType.Transfer)
            .MinAsync(t => (DateTimeOffset?)t.CreatedAt, cancellationToken);

        if (earliestConsumptionAt is null)
        {
            return Result.Ok<DateOnly?>(null);
        }

        var earliestConsumptionDate = LocalCalendarDay.GetDate(earliestConsumptionAt.Value, timeZone);

        var earliestMaterializedDate = await dbContext.DailyItemConsumptions
            .AsNoTracking()
            .MinAsync(c => (DateOnly?)c.Date, cancellationToken);

        return earliestMaterializedDate is { } materialized && materialized <= earliestConsumptionDate
            ? Result.Ok<DateOnly?>(null)
            : Result.Ok<DateOnly?>(earliestConsumptionDate);
    }
}
