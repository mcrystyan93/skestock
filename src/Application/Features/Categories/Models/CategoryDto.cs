namespace skestock.Application.Features.Categories.Models;

public record CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public int ItemCount { get; init; }
    public CategoryIconDto? Icon { get; init; }
    public string? CreatedByName { get; init; }
    public string? LastModifiedByName { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset LastModifiedDate { get; init; }
}

public record CategoryIconDto
{
    public string Name { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
}
