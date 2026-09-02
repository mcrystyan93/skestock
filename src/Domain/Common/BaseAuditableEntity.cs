using skestock.Domain.Entities;

namespace skestock.Domain.Common;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedDate { get; set; }

    public Guid? CreatedById { get; set; }
    public UserProfile? CreatedBy { get; set; }

    public DateTimeOffset LastModifiedDate { get; set; }

    public Guid? LastModifiedById { get; set; }
    public UserProfile? LastModifiedBy { get; set; }
}
