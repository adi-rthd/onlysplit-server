namespace OnlySplit.Application.Features.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string? Message,
    string? Payload,
    bool IsRead,
    DateTime CreatedAt
);