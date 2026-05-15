using System.Text.Json;
using OnlySplit.Infrastructure.Database;
using OnlySplit.Domain.Entities;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.Infrastructure.Services;

public sealed class ActivityService(OnlySplitDbContext context, IRealtimeNotifier realtimeNotifier) : IActivityService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task LogAsync(Guid userId, string type, object metadata, CancellationToken cancellationToken = default)
    {
        var activity = new ActivityLog
        {
            UserId = userId,
            Type = type,
            Metadata = JsonSerializer.Serialize(metadata, SerializerOptions)
        };

        context.ActivityLogs.Add(activity);
        await context.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.SendActivityAsync(userId, "ActivityCreated", new
        {
            activity.Id,
            activity.UserId,
            activity.Type,
            activity.Metadata,
            activity.CreatedAt
        }, cancellationToken);
    }
}
