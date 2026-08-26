using skestock.Application.Common.Caching;
using skestock.Application.Features.SchoolClasses.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SchoolClasses.Commands.CreateSchoolClass;

public class CreateSchoolClassCommand : IRequest<Result<SchoolClassDto>>, ICacheInvalidation
{
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public ClassStatus Status { get; init; } = ClassStatus.Upcoming;

    // Invalidate every cached GetAllSchoolClasses page/filter/sort combination - a new school
    // class can affect any of them (default sort, search matches, filters, etc.).
    public IReadOnlyCollection<string> Tags => [CacheConstants.SchoolClassListTag];
}
