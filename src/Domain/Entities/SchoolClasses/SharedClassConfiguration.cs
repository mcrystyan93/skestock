namespace skestock.Domain.Entities.SchoolClasses;

public class SharedClassConfiguration
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public int InvitationCount { get; set; }
    public int Room4SeatCount { get; set; }
    public int Room2SeatCount { get; set; }
    public int Room6SeatCount { get; set; }
    public ICollection<DepartmentTemplate> DepartmentTemplates { get; set; } = new List<DepartmentTemplate>();
}
