using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Congregations.Models;

namespace skestock.Application.Features.Congregations.Queries.GetCongregationById;

public class GetCongregationByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCongregationByIdQuery, Result<CongregationDto>>
{
    public async ValueTask<Result<CongregationDto>> Handle(GetCongregationByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await dbContext.Congregations
            .AsNoTracking()
            .Where(congregation => congregation.Id == request.Id)
            .Select(congregation => new CongregationDto
            {
                Id = congregation.Id,
                Name = congregation.Name,
                Email = congregation.Email,
                Coordinator = congregation.Coordinator,
                CoordinatorPhone = congregation.CoordinatorPhone,
                DessertContactPerson = congregation.DessertContactPerson,
                DessertContactPhone = congregation.DessertContactPhone,
                CanReceiveStudents = congregation.CanReceiveStudents,
                IsSmallCongregation = congregation.IsSmallCongregation,
                Message = congregation.Message,
                CreatedByName = congregation.CreatedBy != null ? congregation.CreatedBy.FullName : null,
                LastModifiedByName = congregation.LastModifiedBy != null ? congregation.LastModifiedBy.FullName : null,
                CreatedDate = congregation.CreatedDate,
                LastModifiedDate = congregation.LastModifiedDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
            return Result.Fail(new CongregationErrors.CongregationNotFound(request.Id));

        return Result.Ok(dto);
    }
}
