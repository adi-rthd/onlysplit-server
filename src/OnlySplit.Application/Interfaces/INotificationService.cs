using OnlySplit.Application.Features.Notifications;

namespace OnlySplit.Application.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyCollection<NotificationResponse>> GetNotificationsAsync(CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a notification to the database and delivers it in real-time via SignalR.
    /// Notification is retained even if real-time delivery fails.
    /// </summary>
    Task CreateAndSendAsync(
        Guid targetUserId, string type, string title, string message,
        Guid? referenceId = null, Guid? actorUserId = null,
        CancellationToken ct = default);
}