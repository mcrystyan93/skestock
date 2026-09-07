namespace skestock.Application.Features.Locations.Models;

public record LocationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public Guid? ParentLocationId { get; init; }
    public string? ParentLocationName { get; init; }
    public string? CreatedByName { get; init; }
    public string? LastModifiedByName { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset LastModifiedDate { get; init; }
}
