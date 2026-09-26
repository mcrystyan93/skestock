using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetClassLocationStockByItem;

/// <summary>
/// Builds one bar per item the class holds at a location, with one series per category.
/// </summary>
/// <remarks>
/// Each item has a single non-zero value, in its own category's series. Rendered as a stacked
/// bar chart this colours every item bar by category while keeping the same DTO shape as the
/// all-locations chart. Items whose net quantity is zero are left out.
/// </remarks>
public class GetClassLocationStockByItemHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetClassLocationStockByItemQuery, Result<ClassStockByCategoryDto>>
{
    public async ValueTask<Result<ClassStockByCategoryDto>> Handle(
        GetClassLocationStockByItemQuery request,
        CancellationToken cancellationToken)
    {
        var classExists = await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.ClassId, cancellationToken);

        if (!classExists)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.ClassId));

        var locationExists = await dbContext.Locations
            .AsNoTracking()
            .AnyAsync(location => location.Id == request.LocationId, cancellationToken);

        if (!locationExists)
            return Result.Fail(new LocationErrors.LocationNotFound(request.LocationId));

        var stockByItem = await dbContext.StockBatches
            .AsNoTracking()
            .Where(batch => batch.ReceivedClassId == request.ClassId && batch.LocationId == request.LocationId)
            .GroupBy(batch => new
            {
                batch.ItemId,
                ItemName = batch.Item.Name,
                batch.Item.CategoryId,
                CategoryName = batch.Item.Category.Name
            })
            .Select(group => new
            {
                group.Key.ItemId,
                group.Key.ItemName,
                group.Key.CategoryId,
                group.Key.CategoryName,
                Quantity = group.Sum(batch => batch.Quantity)
            })
            .Where(stock => stock.Quantity != 0)
            .ToListAsync(cancellationToken);

        // Items are grouped by category so the bars of one colour sit next to each other.
        var items = stockByItem
            .OrderBy(stock => stock.CategoryName)
            .ThenBy(stock => stock.CategoryId)
            .ThenBy(stock => stock.ItemName)
            .ThenBy(stock => stock.ItemId)
            .ToList();

        var categories = items
            .Select(stock => new { stock.CategoryId, stock.CategoryName })
            .Distinct()
            .ToList();

        return Result.Ok(new ClassStockByCategoryDto
        {
            Labels = [.. items.Select(stock => stock.ItemName)],
            LabelIds = [.. items.Select(stock => stock.ItemId)],
            Series =
            [
                .. categories.Select(category => new ClassStockByCategorySeriesDto
                {
                    Name = category.CategoryName,
                    Data =
                    [
                        .. items.Select(stock => stock.CategoryId == category.CategoryId ? stock.Quantity : 0)
                    ]
                })
            ]
        });
    }
}
