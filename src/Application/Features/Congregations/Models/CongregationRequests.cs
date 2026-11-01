using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.Congregations.Models;

public static class CongregationRequests
{
    public class GetAllCongregationsRequest : BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public class CreateCongregationRequest : CongregationWriteRequest;

    public class UpdateCongregationRequest : CongregationWriteRequest;

    public abstract class CongregationWriteRequest
    {
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? Coordinator { get; init; }
        public string? CoordinatorPhone { get; init; }
        public string? DessertContactPerson { get; init; }
        public string? DessertContactPhone { get; init; }
        public bool CanReceiveStudents { get; init; }
        public bool IsSmallCongregation { get; init; }
        public string? Message { get; init; }
    }
}
