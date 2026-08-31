using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SchoolClasses.Models;

namespace skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassSummary;

public class GetSchoolClassSummaryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetSchoolClassSummaryQuery, Result<SchoolClassSummary>>
{
    public async ValueTask<Result<SchoolClassSummary>> Handle(GetSchoolClassSummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await dbContext.SchoolClasses
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new SchoolClassSummary(
                c.Id,
                c.GoodsReceipts.Count,
                c.GoodsReceipts.Sum(r => (decimal?)r.TotalAmount) ?? 0m))
            .SingleOrDefaultAsync(cancellationToken);

        if (summary is null)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.Id));

        return Result.Ok(summary);
    }
}
