using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlySplit.Application.Features.Notifications;
using OnlySplit.Application.Interfaces;
using OnlySplit.Infrastructure.Database;

namespace OnlySplit.Infrastructure.Services;

public sealed class NotificationService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser,
    IRealtimeNotifier realtimeNotifier,
    ILogger<NotificationService> logger
) : INotificationService
{
    public async Task<IReadOnlyCollection<NotificationResponse>>
        GetNotificationsAsync(
            CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        var notifications = await context.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationResponse(
                x.Id,
                x.Type,
                x.Title,
                x.Message,
                x.IsRead,
                x.CreatedAt,
                x.ReferenceId,
                x.ActorUserId,
                x.ReadAt
            ))
            .ToListAsync(cancellationToken);

        return notifications;
    }

    public async Task MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        var notification = await context.Notifications
            .FirstOrDefaultAsync(
                x =>
                    x.Id == notificationId &&
                    x.UserId == userId,
                cancellationToken);

        if (notification is null)
        {
            throw new Exception("Notification not found.");
        }

        // Idempotent: if already read, return without modifying
        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
    public async Task MarkAllAsReadAsync(
    CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        // Only fetch unread notifications — already-read ones preserve their existing ReadAt
        var notifications = await context.Notifications
            .Where(x =>
                x.UserId == userId &&
                !x.IsRead)
            .ToListAsync(cancellationToken);

        if (notifications.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateAndSendAsync(
        Guid targetUserId, string type, string title, string message,
        Guid? referenceId = null, Guid? actorUserId = null,
        CancellationToken ct = default)
    {
        // 1. Persist notification to database FIRST
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = targetUserId,
            Type = type,
            Title = title,
            Message = message,
            ReferenceId = referenceId,
            ActorUserId = actorUserId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync(ct);

        // 2. Attempt real-time delivery via SignalR (best-effort)
        try
        {
            var payload = new
            {
                notification.Id,
                Type = type,
                Title = title,
                Message = message,
                ReferenceId = referenceId,
                ActorUserId = actorUserId,
                CreatedAt = notification.CreatedAt
            };

            await realtimeNotifier.SendActivityAsync(
                targetUserId, "notification-created", payload, ct);
        }
        catch (Exception ex)
        {
            // Log failure but do NOT throw — notification is already persisted.
            // Settlement persistence must NOT rollback if SignalR delivery fails.
            logger.LogError(ex,
                "Failed to deliver real-time notification {NotificationId} to user {UserId}. " +
                "Notification is persisted and will be available on next fetch.",
                notification.Id, targetUserId);
        }
    }
}