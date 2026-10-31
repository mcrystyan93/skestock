using skestock.Domain.Entities.Congregations;

namespace skestock.Application.Features.Congregations.Models;

public record CongregationMutationDto
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

    public static CongregationMutationDto From(Congregation congregation) => new()
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
        Message = congregation.Message
    };
}
