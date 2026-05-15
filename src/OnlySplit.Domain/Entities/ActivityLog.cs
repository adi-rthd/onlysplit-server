namespace OnlySplit.Domain.Entities;

public class ActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Metadata { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
}
