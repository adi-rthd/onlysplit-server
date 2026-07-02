namespace OnlySplit.Application.Features.Settlements;

/// <summary>
/// Request to record a manual payment against a settlement.
/// </summary>
public sealed record RecordManualPaymentRequest(
    decimal Amount,
    string Method,
    string? TransactionReference,
    string? Notes,
    string? ProofUrl);

/// <summary>
/// Request to reject a pending settlement payment.
/// </summary>
public sealed record RejectPaymentRequest(string Reason);

/// <summary>
/// Individual payment record in settlement payment history.
/// </summary>
public sealed record SettlementPaymentHistoryItem(
    Guid Id,
    decimal Amount,
    string Method,
    string Status,
    string? ProofUrl,
    string? Notes,
    string? TransactionReference,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConfirmedAt,
    string? RejectionReason);
