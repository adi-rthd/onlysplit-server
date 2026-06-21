namespace OnlySplit.Domain.Entities;

public class SettlementAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SettlementPaymentId { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = null!;
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public SettlementPayment SettlementPayment { get; set; } = null!;
    public User User { get; set; } = null!;
}
