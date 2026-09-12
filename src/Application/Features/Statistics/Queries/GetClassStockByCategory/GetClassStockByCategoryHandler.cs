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

        var batchesQuery = dbContext.StockBatches
            .AsNoTracking()
            .Where(b => b.ReceivedClassId == request.ClassId && b.Quantity > 0);

        if (request.LocationId is { } locationId)
            batchesQuery = batchesQuery.Where(b => b.LocationId == locationId);

        var categories = await batchesQuery
            .GroupBy(b => new
            {
                b.Item.CategoryId,
                CategoryName = b.Item.Category.Name
            })
            .Select(g => new CategoryStockSummaryDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                Quantity = g.Sum(b => b.Quantity),
                ItemCount = g.Select(b => b.ItemId).Distinct().Count()
            })
            .OrderBy(c => c.CategoryName)
            .ThenBy(c => c.CategoryId)
            .ToListAsync(cancellationToken);

        return Result.Ok(new ClassStockByCategoryDto
        {
            Categories = categories,
            TotalQuantity = categories.Sum(c => c.Quantity),
            TotalItemCount = categories.Sum(c => c.ItemCount),
            TotalCategoryCount = categories.Count
        });
    }
}
