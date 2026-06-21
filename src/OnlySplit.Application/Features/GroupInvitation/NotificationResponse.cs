namespace OnlySplit.Application.Features.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string? Message,
    bool IsRead,
    DateTime CreatedAt,
    Guid? ReferenceId = null,
    Guid? ActorUserId = null,
    DateTimeOffset? ReadAt = null
);