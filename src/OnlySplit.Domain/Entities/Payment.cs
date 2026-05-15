namespace OnlySplit.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SettlementId { get; set; }
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = Constants.PaymentStatuses.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Settlement? Settlement { get; set; }
}
