using Microsoft.EntityFrameworkCore;
using OnlySplit.Application.Features.Notifications;
using OnlySplit.Application.Interfaces;
using OnlySplit.Infrastructure.Database;

namespace OnlySplit.Infrastructure.Services;

public sealed class NotificationService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser
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
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationResponse(
                x.Id,
                x.Type,
                x.Title,
                x.Message,
                x.Payload,
                $"{x.User.FirstName} {x.User.LastName}",
                x.IsRead,
                x.CreatedAt
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

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;

        await context.SaveChangesAsync(cancellationToken);
    }
    public async Task MarkAllAsReadAsync(
    CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        var notifications = await context.Notifications
            .Where(x =>
                x.UserId == userId &&
                !x.IsRead)
            .ToListAsync(cancellationToken);

        if (notifications.Count == 0)
        {
            return;
        }

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}