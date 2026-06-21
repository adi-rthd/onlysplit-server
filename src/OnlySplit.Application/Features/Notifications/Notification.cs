using OnlySplit.Domain.Entities;

namespace OnlySplit.Application.Features.Notifications;

public class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public string? Payload { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // New fields for enhanced notifications
    public Guid? ReferenceId { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public bool IsArchived { get; set; } = false;
    public DateTimeOffset? ArchivedAt { get; set; }

    // Navigation
    public User? ActorUser { get; set; }
}