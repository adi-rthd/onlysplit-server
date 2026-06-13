using OnlySplit.Application.Features.Notifications;

namespace OnlySplit.Application.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyCollection<NotificationResponse>> GetNotificationsAsync(CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
}