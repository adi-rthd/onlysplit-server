namespace OnlySplit.Domain.Entities;

public class Settlement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? GroupId { get; set; }
    public Guid PayerId { get; set; }
    public Guid ReceiverId { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = Constants.SettlementStatuses.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public Group? Group { get; set; }
    public User? Payer { get; set; }
    public User? Receiver { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<SettlementPayment> SettlementPayments { get; set; } = new List<SettlementPayment>();
}
