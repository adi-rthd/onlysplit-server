using OnlySplit.Domain.Entities;

public class GroupInvitation
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public Guid InvitedBy { get; set; }
    public User InvitedByUser { get; set; } = null!;

    public Guid InvitedUserId { get; set; }
    public User InvitedUser { get; set; } = null!;

    public string Status { get; set; } = "pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}