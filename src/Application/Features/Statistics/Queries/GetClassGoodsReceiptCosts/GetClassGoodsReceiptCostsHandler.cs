using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetClassGoodsReceiptCosts;

public class GetClassGoodsReceiptCostsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetClassGoodsReceiptCostsQuery, Result<ClassGoodsReceiptCostsDto>>
{
    public async ValueTask<Result<ClassGoodsReceiptCostsDto>> Handle(
        GetClassGoodsReceiptCostsQuery request,
        CancellationToken cancellationToken)
    {
        var classExists = await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.ClassId, cancellationToken);

        if (!classExists)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.ClassId));

        var receiptsQuery = dbContext.GoodsReceipts
            .AsNoTracking()
            .Where(r => r.ClassId == request.ClassId);

        if (request.StartDate is { } startDate)
        {
            var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
            receiptsQuery = receiptsQuery.Where(r => r.ReceivedAt >= startDateTime);
        }

        if (request.EndDate is { } endDate)
        {
            var endDateExclusive = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
            receiptsQuery = receiptsQuery.Where(r => r.ReceivedAt < endDateExclusive);
        }

        var points = await receiptsQuery
            .OrderBy(r => r.ReceivedAt)
            .ThenBy(r => r.Id)
            .Select(r => new GoodsReceiptCostPointDto
            {
                Id = r.Id,
                ReceivedAt = r.ReceivedAt,
                TotalAmount = r.TotalAmount,
                SupplierReference = r.SupplierReference
            })
            .ToListAsync(cancellationToken);

        return Result.Ok(new ClassGoodsReceiptCostsDto
        {
            Points = points,
            ReceiptCount = points.Count,
            TotalAmount = points.Sum(p => p.TotalAmount),
            AverageAmount = points.Count == 0 ? 0m : points.Average(p => p.TotalAmount)
        });
    }
}
