namespace skestock.Application.Common.Behaviours;

/// <summary>
/// Marks a command that changes a school class's data. <see cref="ClassOpenBehaviour{TRequest,TResponse}"/> rejects
/// it while that class is closed. Implement exactly one member: the class directly, or through the order list
/// or goods receipt import the command works on.
/// </summary>
public interface IRequiresOpenClass
{
    Guid? ClassId => null;
    Guid? OrderListId => null;
    Guid? GoodsReceiptImportId => null;
}
