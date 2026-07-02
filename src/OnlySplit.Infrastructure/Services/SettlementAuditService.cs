using Microsoft.Extensions.Logging;
using OnlySplit.Application.Interfaces;
using OnlySplit.Domain.Entities;
using OnlySplit.Infrastructure.Database;

namespace OnlySplit.Infrastructure.Services;

/// <summary>
/// Persists settlement audit trail records within the caller's transaction scope.
/// The audit entity is added to the DbContext but NOT saved independently —
/// the caller is responsible for calling SaveChangesAsync within their transaction.
/// </summary>
public sealed class SettlementAuditService(
    OnlySplitDbContext context,
    ILogger<SettlementAuditService> logger) : ISettlementAuditService
{
    public Task RecordAsync(
        Guid settlementPaymentId, Guid userId, string action,
        string? oldStatus = null, string? newStatus = null,
        string? metadataJson = null, CancellationToken cancellationToken = default)
    {
        var audit = new SettlementAudit
        {
            SettlementPaymentId = settlementPaymentId,
            UserId = userId,
            Action = action,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            MetadataJson = metadataJson
        };

        context.SettlementAudits.Add(audit);

        logger.LogDebug("Audit record queued: {Action} for payment {PaymentId} by user {UserId}", action, settlementPaymentId, userId);

        return Task.CompletedTask;
    }
}
