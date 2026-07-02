using OnlySplit.Application.Features.Settlements;
using OnlySplit.Domain.Entities;

namespace OnlySplit.Application.Interfaces;

public interface ISettlementService
{
    // Existing methods
    Task<IReadOnlyCollection<BalanceResponse>> GetBalancesAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SettlementResponse>> GetPendingSettlementsAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Settlement>> RegenerateForGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SettlementResponse>> GetAllPendingSettlementsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SettlementResponse>> GetSettlementSummaryAsync(CancellationToken cancellationToken = default);

    // New: Manual settlement payment workflow

    /// <summary>
    /// Records a manual payment against a settlement (Cash, UPI, BankTransfer).
    /// </summary>
    Task<SettlementPaymentResponse> RecordManualPaymentAsync(Guid settlementId, RecordManualPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the payment history for a settlement ordered by most recent first.
    /// </summary>
    Task<IReadOnlyCollection<SettlementPaymentHistoryItem>> GetPaymentHistoryAsync(Guid settlementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a pending settlement payment (receiver only).
    /// </summary>
    Task<SettlementPaymentResponse> ConfirmPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a pending settlement payment with a reason (receiver only).
    /// </summary>
    Task<SettlementPaymentResponse> RejectPaymentAsync(Guid paymentId, RejectPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending settlement payment (payer only).
    /// </summary>
    Task<SettlementPaymentResponse> CancelPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads proof of payment for a pending settlement payment (payer only).
    /// </summary>
    Task<ProofUploadResponse> UploadProofAsync(Guid paymentId, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a confirmed SettlementPayment record for a Razorpay transaction (shared pipeline).
    /// </summary>
    Task CreateSettlementPaymentForRazorpayAsync(Guid settlementId, decimal amount, string razorpayPaymentId, CancellationToken cancellationToken = default);
}
