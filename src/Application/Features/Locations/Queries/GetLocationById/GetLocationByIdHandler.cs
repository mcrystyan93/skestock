using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Locations.Models;

namespace skestock.Application.Features.Locations.Queries.GetLocationById;

public class GetLocationByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetLocationByIdQuery, Result<LocationDto>>
{
    public async ValueTask<Result<LocationDto>> Handle(GetLocationByIdQuery request, CancellationToken cancellationToken)
    {
        var location = await dbContext.Locations
            .AsNoTracking()
            .Where(l => l.Id == request.Id)
            .Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Type = l.Type,
                ParentLocationId = l.ParentLocationId,
                ParentLocationName = l.ParentLocation != null ? l.ParentLocation.Name : null,
                CreatedByName = l.CreatedBy != null ? l.CreatedBy.FullName : null,
                LastModifiedByName = l.LastModifiedBy != null ? l.LastModifiedBy.FullName : null,
                CreatedDate = l.CreatedDate,
                LastModifiedDate = l.LastModifiedDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (location is null)
            return Result.Fail(new LocationErrors.LocationNotFound(request.Id));

        return Result.Ok(location);
    }
}
