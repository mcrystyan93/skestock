using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SchoolClasses.Models;

public static class SchoolClassRequests
{
    public class GetAllSchoolClassesRequest: BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public class CreateSchoolClassRequest
    {
        public string Name { get; init; } = string.Empty;
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
        public ClassStatus Status { get; init; } = ClassStatus.Upcoming;
    }

    public class UpdateSchoolClassRequest
    {
        public string Name { get; init; } = string.Empty;
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
        public ClassStatus Status { get; init; }
    }
}
