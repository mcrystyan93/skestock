using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetClassItemStockEvolution;

public class GetClassItemStockEvolutionHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetClassItemStockEvolutionQuery, Result<ClassItemStockEvolutionDto>>
{
    public async ValueTask<Result<ClassItemStockEvolutionDto>> Handle(
        GetClassItemStockEvolutionQuery request,
        CancellationToken cancellationToken)
    {
        var classExists = await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(schoolClass => schoolClass.Id == request.ClassId, cancellationToken);

        if (!classExists)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.ClassId));

        // No item selected: the series is the whole class (across all items).
        var item = request.ItemId is { } itemId
            ? await dbContext.Items
                .AsNoTracking()
                .Where(item => item.Id == itemId)
                .Select(item => new { item.Id, item.Name, item.Sku, item.Unit })
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        if (request.ItemId is { } requestedItemId && item is null)
            return Result.Fail(new ItemErrors.ItemNotFound(requestedItemId));

        var transactions = await dbContext.StockTransactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.ClassId == request.ClassId &&
                (request.ItemId == null || transaction.ItemId == request.ItemId) &&
                (request.LocationId == null || transaction.LocationId == request.LocationId))
            .OrderBy(transaction => transaction.CreatedAt)
            .Select(transaction => new { transaction.CreatedAt, transaction.QuantityChange })
            .ToListAsync(cancellationToken);

        var quantityChangesByUtcDate = transactions
            .GroupBy(transaction => DateOnly.FromDateTime(transaction.CreatedAt.UtcDateTime))
            .ToDictionary(
                group => group.Key,
                group => group.Sum(transaction => transaction.QuantityChange));

        var cumulativeQuantity = 0;
        var points = new List<ClassItemStockEvolutionPointDto>(quantityChangesByUtcDate.Count);
        foreach (var (date, quantityChange) in quantityChangesByUtcDate.OrderBy(entry => entry.Key))
        {
            cumulativeQuantity += quantityChange;
            points.Add(new ClassItemStockEvolutionPointDto
            {
                Date = date,
                CumulativeQuantity = cumulativeQuantity
            });
        }

        return Result.Ok(new ClassItemStockEvolutionDto
        {
            ItemId = item?.Id,
            ItemName = item?.Name,
            Sku = item?.Sku,
            Unit = item?.Unit,
            Points = points
        });
    }
}
