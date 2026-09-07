using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Locations.Models;

namespace skestock.Application.Features.Locations.Queries.GetDefaultLocation;

public class GetDefaultLocationHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetDefaultLocationQuery, Result<LocationDto?>>
{
    public async ValueTask<Result<LocationDto?>> Handle(GetDefaultLocationQuery request,
        CancellationToken cancellationToken)
    {
        var location = await dbContext.Locations
            .AsNoTracking()
            .Where(l => l.IsDefault)
            .Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Type = l.Type,
                IsDefault = l.IsDefault,
                ParentLocationId = l.ParentLocationId,
                ParentLocationName = l.ParentLocation != null ? l.ParentLocation.Name : null,
                CreatedByName = l.CreatedBy != null ? l.CreatedBy.FullName : null,
                LastModifiedByName = l.LastModifiedBy != null ? l.LastModifiedBy.FullName : null,
                CreatedDate = l.CreatedDate,
                LastModifiedDate = l.LastModifiedDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        // No default configured is a valid state: return success with a null payload (200 OK null).
        return Result.Ok(location);
    }
}
