namespace OnlySplit.Domain.Entities;

public class GroupMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public Group? Group { get; set; }
    public User? User { get; set; }
}
