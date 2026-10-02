using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Enums;

namespace skestock.Domain.Entities.SchoolClasses;

public class SchoolClass : BaseAuditableEntity, IKeysetEntity
{
    public string Name { get; set; } = null!; // e.g. "Fall 2026 - Cycle 1"
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ClassStatus Status { get; set; } = ClassStatus.Upcoming;
    public int? InvitationCount { get; set; }

    public ICollection<ClassDepartment> Departments { get; set; } = new List<ClassDepartment>();
    public ICollection<StockBatch> BatchesReceived { get; set; } = new List<StockBatch>();
    public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();
    public ICollection<ClassBalance> Balances { get; set; } = new List<ClassBalance>();
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
}
