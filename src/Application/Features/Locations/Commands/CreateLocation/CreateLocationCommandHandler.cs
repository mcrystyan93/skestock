using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Locations.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Locations.Commands.CreateLocation;

public class CreateLocationCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateLocationCommand, Result<LocationDto>>
{
    public async ValueTask<Result<LocationDto>> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
    {
        var location = new Location
        {
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            ParentLocationId = request.ParentLocationId
        };

        dbContext.Locations.Add(location);
        await dbContext.SaveChangesAsync(cancellationToken);

        // ParentLocation/CreatedBy/LastModifiedBy navigations aren't loaded on a freshly-inserted
        // entity (only the *Id FKs are set), so those name fields are resolved with a follow-up
        // lookup here (parent) or left null by design (audit names) - mirrors CreateCategoryCommandHandler.
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
