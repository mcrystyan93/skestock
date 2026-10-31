namespace skestock.Application.Features.Congregations.Models;

public record CongregationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Coordinator { get; init; }
    public string? CoordinatorPhone { get; init; }
    public string? DessertContactPerson { get; init; }
    public string? DessertContactPhone { get; init; }
    public bool CanReceiveStudents { get; init; }
    public bool IsSmallCongregation { get; init; }
    public string? Message { get; init; }
    public string? CreatedByName { get; init; }
    public string? LastModifiedByName { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset LastModifiedDate { get; init; }
}
