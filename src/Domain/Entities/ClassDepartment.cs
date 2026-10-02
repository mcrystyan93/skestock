using skestock.Domain.Common;

namespace skestock.Domain.Entities;

public class ClassDepartment : BaseEntity
{
    public Guid SchoolClassId { get; set; }
    public SchoolClass SchoolClass { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Responsibilities { get; set; } = string.Empty;
    public string? ResponsiblePerson { get; set; }
}
