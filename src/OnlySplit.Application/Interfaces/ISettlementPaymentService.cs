using OnlySplit.Application.Features.Settlements;

namespace OnlySplit.Application.Interfaces;

public interface ISettlementPaymentService
{
    Task<SettlementPaymentResponse> CreatePaymentRequestAsync(
        CreateSettlementPaymentRequest request, CancellationToken cancellationToken = default);

    Task<ProofUploadResponse> UploadProofAsync(
        Guid paymentId, Stream fileStream, string fileName, string contentType,
        long fileSize, CancellationToken cancellationToken = default);

    Task<SettlementPaymentResponse> ConfirmPaymentAsync(
        Guid paymentId, CancellationToken cancellationToken = default);

    Task<SettlementPaymentResponse> RejectPaymentAsync(
        Guid paymentId, string reason, CancellationToken cancellationToken = default);

    Task<SettlementHistoryResponse> GetHistoryAsync(
        Guid settlementId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SettlementSummaryResponse>> GetGroupSummaryAsync(
        Guid groupId, CancellationToken cancellationToken = default);
}
