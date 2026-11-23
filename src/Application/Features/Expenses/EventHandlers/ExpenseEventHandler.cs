using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Enums;
using skestock.Domain.Events.Expenses;

namespace skestock.Application.Features.Expenses.EventHandlers;

// Pushes status changes to everyone viewing the class; the client reloads its list.
public class ExpenseEventHandler(IRealtimeNotifier notifier)
    : INotificationHandler<ExpenseCreatedEvent>,
      INotificationHandler<ExpenseProcessedEvent>,
      INotificationHandler<ExpenseFailedEvent>
{
    public ValueTask Handle(ExpenseCreatedEvent notification, CancellationToken cancellationToken) =>
        Notify(RealtimeEvents.ExpenseCreated, notification.ExpenseId, notification.ClassId, ExpenseStatus.Processing, cancellationToken);

    public ValueTask Handle(ExpenseProcessedEvent notification, CancellationToken cancellationToken) =>
        Notify(RealtimeEvents.ExpenseProcessed, notification.ExpenseId, notification.ClassId, ExpenseStatus.Processed, cancellationToken);

    public ValueTask Handle(ExpenseFailedEvent notification, CancellationToken cancellationToken) =>
        Notify(RealtimeEvents.ExpenseFailed, notification.ExpenseId, notification.ClassId, ExpenseStatus.Failed, cancellationToken);

    private async ValueTask Notify(string eventName, Guid expenseId, Guid classId, ExpenseStatus status, CancellationToken cancellationToken) =>
        await notifier.NotifyGroupAsync(
            RealtimeGroups.SchoolClass(classId),
            eventName,
            new { ExpenseId = expenseId, ClassId = classId, Status = status },
            cancellationToken);
}
