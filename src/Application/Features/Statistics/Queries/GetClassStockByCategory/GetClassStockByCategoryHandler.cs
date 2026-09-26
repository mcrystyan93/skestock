using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetClassStockByCategory;

/// <summary>
/// Builds one bar per location that holds class stock, stacked by category.
/// Locations without any class batch are omitted; categories are zero-filled per location.
/// </summary>
public class GetClassStockByCategoryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetClassStockByCategoryQuery, Result<ClassStockByCategoryDto>>
{
    public async ValueTask<Result<ClassStockByCategoryDto>> Handle(
        GetClassStockByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        var classExists = await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.ClassId, cancellationToken);

        if (!classExists)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.ClassId));

        var stockByCategoryAndLocation = await dbContext.StockBatches
            .AsNoTracking()
            .Where(batch => batch.ReceivedClassId == request.ClassId)
            .GroupBy(batch => new
            {
                batch.Item.CategoryId,
                CategoryName = batch.Item.Category.Name,
                batch.LocationId,
                LocationName = batch.Location.Name
            })
            .Select(group => new
            {
                group.Key.CategoryId,
                group.Key.CategoryName,
                group.Key.LocationId,
                group.Key.LocationName,
                Quantity = group.Sum(batch => batch.Quantity)
            })
            .ToListAsync(cancellationToken);

        var categories = stockByCategoryAndLocation
            .Select(stock => new { stock.CategoryId, stock.CategoryName })
            .Distinct()
            .OrderBy(category => category.CategoryName)
            .ThenBy(category => category.CategoryId)
            .ToList();

        var locations = stockByCategoryAndLocation
            .Select(stock => new { stock.LocationId, stock.LocationName })
            .Distinct()
            .OrderBy(location => location.LocationName)
            .ThenBy(location => location.LocationId)
            .ToList();

        var quantities = stockByCategoryAndLocation.ToDictionary(
            stock => (stock.CategoryId, stock.LocationId),
            stock => stock.Quantity);

        return Result.Ok(new ClassStockByCategoryDto
        {
            Labels = [.. locations.Select(location => location.LocationName)],
            LabelIds = [.. locations.Select(location => location.LocationId)],
            Series =
            [
                .. categories.Select(category => new ClassStockByCategorySeriesDto
                {
                    Name = category.CategoryName,
                    Data =
                    [
                        .. locations.Select(location =>
                            quantities.GetValueOrDefault((category.CategoryId, location.LocationId)))
                    ]
                })
            ]
        });
    }
}
