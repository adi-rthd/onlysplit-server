namespace OnlySplit.Domain.Entities;

public class SettlementPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SettlementId { get; set; }
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = Constants.SettlementPaymentStatuses.PendingConfirmation;
    public string Method { get; set; } = null!;
    public string? ProofUrl { get; set; }
    public string? ProofFileName { get; set; }
    public long? ProofFileSize { get; set; }
    public DateTimeOffset? ProofUploadedAt { get; set; }
    public string? UpiReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Concurrency token to prevent race conditions during confirm/reject operations.
    /// </summary>
    public uint RowVersion { get; set; }

    // Navigation
    public Settlement Settlement { get; set; } = null!;
    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
}
