namespace OnlySplit.Application.Interfaces;

public interface ISettlementAuditService
{
    Task RecordAsync(
        Guid settlementPaymentId, Guid userId, string action,
        string? oldStatus = null, string? newStatus = null,
        string? metadataJson = null, CancellationToken cancellationToken = default);
}
