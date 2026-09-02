namespace skestock.Domain.Entities;

public class UserProfile: BaseAuditableEntity, IKeysetEntity
{
    // Links to AspNetUsers.Id in the Infrastructure layer
    public required Guid IdentityId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    
    public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();

}
