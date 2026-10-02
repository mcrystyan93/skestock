using skestock.Domain.Common;

namespace skestock.Domain.Entities;

public class DepartmentTemplate : BaseEntity
{
    public int SharedClassConfigurationId { get; set; }
    public SharedClassConfiguration SharedClassConfiguration { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Responsibilities { get; set; } = string.Empty;
}
