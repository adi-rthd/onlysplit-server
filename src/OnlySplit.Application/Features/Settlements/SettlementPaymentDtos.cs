namespace OnlySplit.Application.Features.Settlements;

public sealed record CreateSettlementPaymentRequest(
    Guid SettlementId,
    decimal Amount,
    string Method,
    string? UpiReferenceNumber,
    string? Notes);

public sealed record SettlementPaymentResponse(
    Guid Id,
    Guid SettlementId,
    Guid FromUserId,
    Guid ToUserId,
    decimal Amount,
    string Status,
    string Method,
    string? ProofUrl,
    string? UpiReferenceNumber,
    string? Notes,
    DateTimeOffset? ConfirmedAt,
    string? RejectionReason,
    DateTimeOffset CreatedAt);

public sealed record ProofUploadResponse(
    string ProofUrl,
    string ProofFileName,
    long ProofFileSize,
    DateTimeOffset ProofUploadedAt);

public sealed record SettlementHistoryResponse(
    decimal TotalAmount,
    decimal ConfirmedAmount,
    decimal PendingAmount,
    decimal RejectedAmount,
    decimal RemainingAmount,
    decimal ProgressPercentage,
    IReadOnlyCollection<SettlementPaymentResponse> Payments);

public sealed record SettlementSummaryResponse(
    Guid SettlementId,
    Guid PayerId,
    Guid ReceiverId,
    decimal TotalAmount,
    decimal ConfirmedAmount,
    decimal RemainingAmount,
    decimal ProgressPercentage,
    string Status,
    DateTimeOffset? LatestPaymentDate);
