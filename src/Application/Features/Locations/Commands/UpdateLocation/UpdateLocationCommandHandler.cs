using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Locations.Models;

namespace skestock.Application.Features.Locations.Commands.UpdateLocation;

public class UpdateLocationCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateLocationCommand, Result<LocationDto>>
{
    public async ValueTask<Result<LocationDto>> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var location = await dbContext.Locations
            .SingleOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        if (location is null)
            return Result.Fail(new LocationErrors.LocationNotFound(request.Id));

        location.Name = request.Name.Trim();
        location.Type = request.Type.Trim();
        location.ParentLocationId = request.ParentLocationId;

        await dbContext.SaveChangesAsync(cancellationToken);

        // ParentLocation navigation isn't guaranteed to be loaded on the tracked entity above
        // (only the FK is set), so the parent's name is resolved with a follow-up lookup here -
        // mirrors CreateLocationCommandHandler.
        var parentName = request.ParentLocationId.HasValue
            ? await dbContext.Locations
                .AsNoTracking()
                .Where(l => l.Id == request.ParentLocationId)
                .Select(l => l.Name)
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        return Result.Ok(new LocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Type = location.Type,
            ParentLocationId = location.ParentLocationId,
            ParentLocationName = parentName,
            CreatedByName = location.CreatedBy?.FullName,
            LastModifiedByName = location.LastModifiedBy?.FullName,
            CreatedDate = location.CreatedDate,
            LastModifiedDate = location.LastModifiedDate
        });
    }
}
