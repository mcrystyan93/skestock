using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetClassStockByCategory;

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

        var classBatchesQuery = dbContext.StockBatches
            .AsNoTracking()
            .Where(batch => batch.ReceivedClassId == request.ClassId);

        if (request.LocationId is { } locationId)
        {
            var locationName = await dbContext.Locations
                .AsNoTracking()
                .Where(location => location.Id == locationId)
                .Select(location => location.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (locationName is null)
                return Result.Fail(new LocationErrors.LocationNotFound(locationId));

            var categories = await classBatchesQuery
                .Select(batch => new { batch.Item.CategoryId, CategoryName = batch.Item.Category.Name })
                .Distinct()
                .OrderBy(category => category.CategoryName)
                .ThenBy(category => category.CategoryId)
                .ToListAsync(cancellationToken);

            var quantitiesByCategory = await classBatchesQuery
                .Where(batch => batch.LocationId == locationId)
                .GroupBy(batch => new { batch.Item.CategoryId, CategoryName = batch.Item.Category.Name })
                .Select(group => new { group.Key.CategoryId, Quantity = group.Sum(batch => batch.Quantity) })
                .ToDictionaryAsync(
                    category => category.CategoryId,
                    category => category.Quantity,
                    cancellationToken);

            return Result.Ok(new ClassStockByCategoryDto
            {
                Labels = [locationName],
                Series =
                [
                    .. categories.Select(category => new ClassStockByCategorySeriesDto
                    {
                        Name = category.CategoryName,
                        Data = [quantitiesByCategory.GetValueOrDefault(category.CategoryId)]
                    })
                ]
            });
        }

        var stockByCategoryAndLocation = await classBatchesQuery
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

        var allCategories = stockByCategoryAndLocation
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

        var quantitiesByCategoryAndLocation = stockByCategoryAndLocation
            .ToDictionary(
                stock => (stock.CategoryId, stock.LocationId),
                stock => stock.Quantity);

        return Result.Ok(new ClassStockByCategoryDto
        {
            Labels = [.. locations.Select(location => location.LocationName)],
            Series =
            [
                .. allCategories.Select(category => new ClassStockByCategorySeriesDto
                {
                    Name = category.CategoryName,
                    Data =
                    [
                        .. locations.Select(location =>
                            quantitiesByCategoryAndLocation.GetValueOrDefault(
                                (category.CategoryId, location.LocationId)))
                    ]
                })
            ]
        });
    }
}
