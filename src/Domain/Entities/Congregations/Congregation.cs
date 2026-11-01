namespace skestock.Domain.Entities.Congregations;

public class Congregation : BaseAuditableEntity, IKeysetEntity
{
    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Coordinator { get; set; }

    public string? CoordinatorPhone { get; set; }

    public string? DessertContactPerson { get; set; }

    public string? DessertContactPhone { get; set; }

    public bool CanReceiveStudents { get; set; }

    public bool IsSmallCongregation { get; set; }

    public string? Message { get; set; }

    // Soft delete: null means active.
    public DateTimeOffset? DeletedDate { get; set; }
}
