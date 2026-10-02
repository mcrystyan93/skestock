namespace skestock.Domain.Entities;

public class SharedClassConfiguration
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public int InvitationCount { get; set; }
    public ICollection<DepartmentTemplate> DepartmentTemplates { get; set; } = new List<DepartmentTemplate>();
}
