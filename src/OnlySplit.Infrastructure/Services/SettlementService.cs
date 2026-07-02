using Microsoft.EntityFrameworkCore;
using OnlySplit.Domain.Constants;
using OnlySplit.Infrastructure.Database;
using OnlySplit.Application.Features.Settlements;
using OnlySplit.Domain.Entities;
using OnlySplit.Domain.Exceptions;
using OnlySplit.Application.Interfaces;
using OnlySplit.Domain.Utils;

namespace OnlySplit.Infrastructure.Services;

public sealed class SettlementService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser,
    IActivityService activityService,
    ISettlementAuditService auditService,
    INotificationService notificationService,
    IRealtimeNotifier realtimeNotifier
    ) : ISettlementService
{
    public async Task<IReadOnlyCollection<BalanceResponse>> GetBalancesAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(groupId, cancellationToken);
        var (members, balances) = await CalculateBalancesAsync(groupId, cancellationToken);

        return members.Select(member => new BalanceResponse(
                member.UserId,
                member.FirstName,
                member.LastName,
                member.Email,
                MoneyMath.Round(balances.GetValueOrDefault(member.UserId))))
            .OrderByDescending(balance => balance.NetBalance)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<SettlementResponse>> GetPendingSettlementsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(groupId, cancellationToken);

        var settlements = await context.Settlements
            .AsNoTracking()
            .Include(settlement => settlement.Payer)
            .Include(settlement => settlement.Receiver)
            .Where(settlement => settlement.GroupId == groupId && settlement.Status == SettlementStatuses.Pending)
            .OrderBy(settlement => settlement.CreatedAt)
            .ToListAsync(cancellationToken);

        return settlements.Select(ToResponse).ToArray();
    }

    public async Task<IReadOnlyCollection<Settlement>> RegenerateForGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(groupId, cancellationToken);

        var pendingSettlements = await context.Settlements
            .Include(settlement => settlement.Payments)
            .Where(settlement => settlement.GroupId == groupId && settlement.Status == SettlementStatuses.Pending)
            .ToListAsync(cancellationToken);

        foreach (var settlement in pendingSettlements)
        {
            settlement.Status = SettlementStatuses.Cancelled;
            foreach (var payment in settlement.Payments.Where(payment => payment.Status == PaymentStatuses.Pending))
            {
                payment.Status = PaymentStatuses.Cancelled;
            }
        }

        var (_, balances) = await CalculateBalancesAsync(groupId, cancellationToken);
        var settlements = BuildOptimizedSettlements(groupId, balances);

        if (settlements.Count > 0)
        {
            context.Settlements.AddRange(settlements);
        }

        await context.SaveChangesAsync(cancellationToken);

        await activityService.LogAsync(currentUser.UserId, ActivityTypes.SettlementsGenerated, new
        {
            GroupId = groupId,
            Count = settlements.Count,
            Total = settlements.Sum(settlement => settlement.Amount)
        }, cancellationToken);

        return settlements;
    }

    private async Task EnsureMemberAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var groupExists = await context.Groups.AnyAsync(group => group.Id == groupId, cancellationToken);
        if (!groupExists)
        {
            throw new NotFoundException("Group was not found.");
        }

        var isMember = await context.GroupMembers
            .AnyAsync(member => member.GroupId == groupId && member.UserId == currentUser.UserId, cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenException("You are not a member of this group.");
        }
    }

    private async Task<(IReadOnlyCollection<MemberBalanceProjection> Members, Dictionary<Guid, decimal> Balances)> CalculateBalancesAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var members = await context.GroupMembers
            .AsNoTracking()
            .Where(member => member.GroupId == groupId)
            .Select(member => new MemberBalanceProjection(
                member.UserId,
                member.User!.FirstName,
                member.User.LastName,
                member.User.Email))
            .ToListAsync(cancellationToken);

        var balances = members.ToDictionary(member => member.UserId, _ => 0m);

        var expenses = await context.Expenses
            .AsNoTracking()
            .Include(expense => expense.Splits)
            .Where(expense => expense.GroupId == groupId)
            .ToListAsync(cancellationToken);

        foreach (var expense in expenses)
        {
            balances[expense.PaidBy] = balances.GetValueOrDefault(expense.PaidBy) + expense.Amount;

            foreach (var split in expense.Splits)
            {
                balances[split.UserId] = balances.GetValueOrDefault(split.UserId) - split.AmountOwed;
            }
        }

        var completedSettlements = await context.Settlements
            .AsNoTracking()
            .Where(settlement => settlement.GroupId == groupId && settlement.Status == SettlementStatuses.Settled)
            .ToListAsync(cancellationToken);

        foreach (var settlement in completedSettlements)
        {
            balances[settlement.PayerId] = balances.GetValueOrDefault(settlement.PayerId) + settlement.Amount;
            balances[settlement.ReceiverId] = balances.GetValueOrDefault(settlement.ReceiverId) - settlement.Amount;
        }

        return (members, balances);
    }

    private static List<Settlement> BuildOptimizedSettlements(Guid groupId, IReadOnlyDictionary<Guid, decimal> balances)
    {
        var debtors = balances
            .Where(balance => balance.Value < -0.01m)
            .Select(balance => new BalanceBucket(balance.Key, MoneyMath.Round(Math.Abs(balance.Value))))
            .OrderByDescending(balance => balance.Amount)
            .ToList();

        var creditors = balances
            .Where(balance => balance.Value > 0.01m)
            .Select(balance => new BalanceBucket(balance.Key, MoneyMath.Round(balance.Value)))
            .OrderByDescending(balance => balance.Amount)
            .ToList();

        var settlements = new List<Settlement>();
        var debtorIndex = 0;
        var creditorIndex = 0;

        while (debtorIndex < debtors.Count && creditorIndex < creditors.Count)
        {
            var debtor = debtors[debtorIndex];
            var creditor = creditors[creditorIndex];
            var amount = MoneyMath.Round(Math.Min(debtor.Amount, creditor.Amount));

            if (amount > 0)
            {
                settlements.Add(new Settlement
                {
                    GroupId = groupId,
                    PayerId = debtor.UserId,
                    ReceiverId = creditor.UserId,
                    Amount = amount
                });
            }

            debtors[debtorIndex] = debtor with { Amount = MoneyMath.Round(debtor.Amount - amount) };
            creditors[creditorIndex] = creditor with { Amount = MoneyMath.Round(creditor.Amount - amount) };

            if (debtors[debtorIndex].Amount <= 0.01m)
            {
                debtorIndex++;
            }

            if (creditors[creditorIndex].Amount <= 0.01m)
            {
                creditorIndex++;
            }
        }

        return settlements;
    }
    public async Task<IReadOnlyCollection<SettlementResponse>> GetAllPendingSettlementsAsync(
        CancellationToken cancellationToken = default)
    {
        var settlements = await context.Settlements
            .AsNoTracking()
            .Include(s => s.Payer)
            .Include(s => s.Receiver)
            .Include(s => s.Group)
            .Where(s =>
                s.Status == SettlementStatuses.Pending &&
                s.Group!.Members.Any(m => m.UserId == currentUser.UserId))
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var merged = settlements
            .GroupBy(s => new
            {
                s.PayerId,
                s.ReceiverId
            })
            .Select(g =>
            {
                var first = g.First();

                return new SettlementResponse(
                    Guid.Empty,
                    Guid.Empty,
                    first.PayerId,
                    $"{first.Payer!.FirstName} {first.Payer.LastName}".Trim(),
                    first.ReceiverId,
                    $"{first.Receiver!.FirstName} {first.Receiver.LastName}".Trim(),
                    g.Sum(x => x.Amount),
                    SettlementStatuses.Pending,
                    g.Min(x => x.CreatedAt));
            })
            .OrderByDescending(x => x.Amount)
            .ToArray();

        return merged;
    }
    // public async Task<IReadOnlyCollection<SettlementOverviewResponse>> GetSettlementSummaryAsync(CancellationToken cancellationToken = default)
    // {
    //     var settlements = await context.Settlements
    //         .AsNoTracking()
    //         .Include(s => s.Payer)
    //         .Include(s => s.Receiver)
    //         .Where(s =>
    //             s.Status == SettlementStatuses.Pending &&
    //             context.GroupMembers.Any(gm =>
    //                 gm.GroupId == s.GroupId &&
    //                 gm.UserId == currentUser.UserId))
    //         .ToListAsync(cancellationToken);

    //     return settlements
    //         .GroupBy(s => new
    //         {
    //             s.PayerId,
    //             s.ReceiverId,
    //             PayerName = $"{s.Payer!.FirstName} {s.Payer.LastName}".Trim(),
    //             ReceiverName = $"{s.Receiver!.FirstName} {s.Receiver.LastName}".Trim()
    //         })
    //         .Select(g => new SettlementOverviewResponse(
    //             g.Key.PayerId,
    //             g.Key.PayerName,
    //             g.Key.ReceiverId,
    //             g.Key.ReceiverName,
    //             g.Sum(x => x.Amount)
    //         ))
    //         .OrderByDescending(x => x.Amount)
    //         .ToArray();
    // }
    public async Task<IReadOnlyCollection<SettlementResponse>> GetSettlementSummaryAsync(CancellationToken cancellationToken = default)
    {
        var settlements = await context.Settlements
            .AsNoTracking()
            .Include(s => s.Payer)
            .Include(s => s.Receiver)
            .Where(s =>
                s.Status == SettlementStatuses.Pending &&
                context.GroupMembers.Any(gm =>
                    gm.GroupId == s.GroupId &&
                    gm.UserId == currentUser.UserId))
            .OrderByDescending(s => s.Amount)
            .ToListAsync(cancellationToken);

        // Instead of GroupBy, we just map each individual row to a response.
        // This preserves the unique 'Id' so Razorpay knows exactly what is being paid!
        return settlements.Select(ToResponse).ToArray();
    }
    public async Task<SettlementPaymentResponse> RecordManualPaymentAsync(Guid settlementId, RecordManualPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var settlement = await GetSettlementOrThrowAsync(settlementId, cancellationToken);

        if (currentUser.UserId != settlement.PayerId)
            throw new ForbiddenException("Only the settlement payer can record payments.");

        if (settlement.Status is SettlementStatuses.Settled or SettlementStatuses.Cancelled)
            throw new AppException("Cannot make payments against a completed or cancelled settlement.");

        if (request.Amount <= 0)
            throw new AppException("Amount must be greater than zero.");

        var remaining = settlement.Amount - settlement.PaidAmount;
        if (request.Amount > remaining)
            throw new AppException($"Amount cannot exceed the remaining balance of {remaining}.");

        if (!SettlementPaymentMethods.ManualMethods.Contains(request.Method))
            throw new AppException("Invalid payment method. Allowed: Cash, UPI, BankTransfer");

        var hasPending = await context.SettlementPayments
            .AnyAsync(sp => sp.SettlementId == settlementId && sp.Status == SettlementPaymentStatuses.PendingConfirmation, cancellationToken);
        if (hasPending)
            throw new ConflictException("A pending payment already exists. Wait for confirmation or rejection.");

        var payment = new SettlementPayment
        {
            SettlementId = settlementId,
            FromUserId = currentUser.UserId,
            ToUserId = settlement.ReceiverId,
            Amount = request.Amount,
            Status = SettlementPaymentStatuses.PendingConfirmation,
            Method = request.Method,
            UpiReferenceNumber = request.TransactionReference,
            Notes = request.Notes
        };

        context.SettlementPayments.Add(payment);

        // Audit: payment recorded (participates in same save)
        await CreateAuditAsync(payment.Id, "PaymentAdded",
            newStatus: SettlementPaymentStatuses.PendingConfirmation, ct: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        // Notification: inform receiver (best-effort, after save)
        await SendNotificationAsync(
            settlement.ReceiverId,
            "settlement_payment_submitted",
            "Payment Submitted",
            $"A payment of {payment.Amount:C} has been submitted for your settlement.",
            payment.SettlementId,
            cancellationToken);

        // SignalR: broadcast to both parties
        await BroadcastSettlementEventAsync(settlement, "SettlementPaymentSubmitted", new
        {
            payment.Id,
            payment.SettlementId,
            payment.Amount,
            payment.Method,
            payment.Status
        }, cancellationToken);

        return ToPaymentResponse(payment);
    }

    public async Task<IReadOnlyCollection<SettlementPaymentHistoryItem>> GetPaymentHistoryAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        var settlement = await context.Settlements
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == settlementId, cancellationToken)
            ?? throw new NotFoundException("Settlement not found.");

        if (currentUser.UserId != settlement.PayerId && currentUser.UserId != settlement.ReceiverId)
            throw new ForbiddenException("You do not have permission to view this settlement's payment history.");

        // Note: UpiReferenceNumber field stores generic transaction references (UPI ref, bank UTR, Razorpay ID).
        // A future migration will rename this column to TransactionReference for clarity.
        var payments = await context.SettlementPayments
            .AsNoTracking()
            .Where(sp => sp.SettlementId == settlementId)
            .OrderByDescending(sp => sp.CreatedAt)
            .Select(sp => new SettlementPaymentHistoryItem(
                sp.Id,
                sp.Amount,
                sp.Method,
                sp.Status,
                sp.ProofUrl,
                sp.Notes,
                sp.UpiReferenceNumber,
                sp.CreatedAt,
                sp.ConfirmedAt,
                sp.RejectionReason))
            .ToListAsync(cancellationToken);

        return payments;
    }

    public async Task<SettlementPaymentResponse> ConfirmPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await GetPaymentOrThrowAsync(paymentId, cancellationToken);

        if (currentUser.UserId != payment.ToUserId)
            throw new ForbiddenException("Only the settlement receiver can confirm payments.");

        if (payment.Status == SettlementPaymentStatuses.Confirmed)
            throw new AppException("Payment is already confirmed.");

        if (payment.Status == SettlementPaymentStatuses.Rejected)
            throw new AppException("A rejected payment cannot be confirmed.");

        if (payment.Status != SettlementPaymentStatuses.PendingConfirmation)
            throw new AppException("Only pending payments can be confirmed.");

        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            payment.Status = SettlementPaymentStatuses.Confirmed;
            payment.ConfirmedAt = DateTimeOffset.UtcNow;
            payment.ConfirmedBy = currentUser.UserId;

            await RecalculateSettlementInternalAsync(payment.Settlement, cancellationToken);

            // Audit: participates in transaction (before save)
            await CreateAuditAsync(payment.Id, "PaymentConfirmed",
                oldStatus: SettlementPaymentStatuses.PendingConfirmation,
                newStatus: SettlementPaymentStatuses.Confirmed, ct: cancellationToken);

            if (payment.Settlement.Status == SettlementStatuses.Settled)
            {
                await CreateAuditAsync(payment.Id, "SettlementCompleted",
                    newStatus: SettlementStatuses.Settled, ct: cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        // Notification: inform payer of confirmation
        await SendNotificationAsync(
            payment.FromUserId,
            "settlement_payment_confirmed",
            "Payment Confirmed",
            $"Your payment of {payment.Amount:C} has been confirmed.",
            payment.SettlementId,
            cancellationToken);

        // Notification: settlement completed
        if (payment.Settlement.Status == SettlementStatuses.Settled)
        {
            await SendNotificationAsync(
                payment.FromUserId,
                "settlement_completed",
                "Settlement Completed",
                "Your settlement has been fully paid and marked as complete.",
                payment.SettlementId,
                cancellationToken);
        }

        // SignalR: broadcast after commit
        await BroadcastSettlementEventAsync(payment.Settlement, "SettlementPaymentConfirmed", new
        {
            payment.Id,
            payment.SettlementId,
            payment.Amount,
            payment.Status,
            SettlementStatus = payment.Settlement.Status,
            payment.Settlement.PaidAmount
        }, cancellationToken);

        return ToPaymentResponse(payment);
    }

    public async Task<SettlementPaymentResponse> RejectPaymentAsync(Guid paymentId, RejectPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var payment = await GetPaymentOrThrowAsync(paymentId, cancellationToken);

        if (currentUser.UserId != payment.ToUserId)
            throw new ForbiddenException("Only the settlement receiver can reject payments.");

        if (payment.Status != SettlementPaymentStatuses.PendingConfirmation)
            throw new AppException("Only pending payments can be rejected.");

        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            payment.Status = SettlementPaymentStatuses.Rejected;
            payment.RejectionReason = request.Reason;
            payment.UpdatedAt = DateTimeOffset.UtcNow;

            // Audit: participates in transaction
            await CreateAuditAsync(payment.Id, "PaymentRejected",
                oldStatus: SettlementPaymentStatuses.PendingConfirmation,
                newStatus: SettlementPaymentStatuses.Rejected,
                metadataJson: System.Text.Json.JsonSerializer.Serialize(new { reason = request.Reason }),
                ct: cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        // Notification: inform payer of rejection
        await SendNotificationAsync(
            payment.FromUserId,
            "settlement_payment_rejected",
            "Payment Rejected",
            $"Your payment of {payment.Amount:C} was rejected. Reason: {request.Reason}",
            payment.SettlementId,
            cancellationToken);

        // SignalR: broadcast after commit
        await BroadcastSettlementEventAsync(payment.Settlement, "SettlementPaymentRejected", new
        {
            payment.Id,
            payment.SettlementId,
            payment.Amount,
            payment.Status,
            payment.RejectionReason
        }, cancellationToken);

        return ToPaymentResponse(payment);
    }

    public async Task<SettlementPaymentResponse> CancelPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await GetPaymentOrThrowAsync(paymentId, cancellationToken);

        if (currentUser.UserId != payment.FromUserId)
            throw new ForbiddenException("Only the settlement payer can cancel payments.");

        if (payment.Status != SettlementPaymentStatuses.PendingConfirmation)
            throw new AppException("Only pending payments can be cancelled.");

        payment.Status = SettlementPaymentStatuses.Rejected;
        payment.RejectionReason = "Cancelled by payer";
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        // Audit: participates in same save
        await CreateAuditAsync(payment.Id, "PaymentCancelled",
            oldStatus: SettlementPaymentStatuses.PendingConfirmation,
            newStatus: SettlementPaymentStatuses.Rejected,
            metadataJson: System.Text.Json.JsonSerializer.Serialize(new { reason = "Cancelled by payer" }),
            ct: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        // SignalR: broadcast cancellation
        await BroadcastSettlementEventAsync(payment.Settlement, "SettlementPaymentCancelled", new
        {
            payment.Id,
            payment.SettlementId,
            payment.Status
        }, cancellationToken);

        return ToPaymentResponse(payment);
    }

    public Task<ProofUploadResponse> UploadProofAsync(Guid paymentId, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <summary>
    /// Creates a confirmed SettlementPayment for a gateway (Razorpay) transaction.
    /// Uses the same settlement lifecycle pipeline as manual payments:
    /// SettlementPayment → Recalculation → Audit → Save (atomic) → Notifications + SignalR (best-effort).
    /// This method is idempotent — duplicate calls with the same razorpayPaymentId are safely ignored.
    /// </summary>
    public async Task CreateSettlementPaymentForRazorpayAsync(
        Guid settlementId, decimal amount, string razorpayPaymentId, CancellationToken cancellationToken = default)
    {
        // Idempotency: check if a SettlementPayment already exists for this Razorpay payment
        var alreadyProcessed = await context.SettlementPayments
            .AnyAsync(sp => sp.SettlementId == settlementId
                && sp.UpiReferenceNumber == razorpayPaymentId
                && sp.Method == SettlementPaymentMethods.Razorpay, cancellationToken);

        if (alreadyProcessed)
            return; // Idempotent: already processed, nothing to do

        var settlement = await context.Settlements
            .FirstOrDefaultAsync(s => s.Id == settlementId, cancellationToken)
            ?? throw new NotFoundException("Settlement not found.");

        // Create SettlementPayment as Confirmed immediately (gateway verified)
        var settlementPayment = new SettlementPayment
        {
            SettlementId = settlementId,
            FromUserId = settlement.PayerId,
            ToUserId = settlement.ReceiverId,
            Amount = amount,
            Status = SettlementPaymentStatuses.Confirmed,
            Method = SettlementPaymentMethods.Razorpay,
            UpiReferenceNumber = razorpayPaymentId,
            ConfirmedAt = DateTimeOffset.UtcNow,
            ConfirmedBy = settlement.PayerId
        };

        context.SettlementPayments.Add(settlementPayment);

        // Recalculate settlement using shared pipeline
        await RecalculateSettlementInternalAsync(settlement, cancellationToken);

        // Audit: participates in same transaction (before save)
        await CreateAuditAsync(settlementPayment.Id, "PaymentConfirmed",
            newStatus: SettlementPaymentStatuses.Confirmed, ct: cancellationToken);

        if (settlement.Status == SettlementStatuses.Settled)
        {
            await CreateAuditAsync(settlementPayment.Id, "SettlementCompleted",
                newStatus: SettlementStatuses.Settled, ct: cancellationToken);
        }

        // Save: SettlementPayment + Settlement update + Audit — all atomic
        // NOTE: The caller (PaymentService) owns the transaction. We only add entities and save.
        await context.SaveChangesAsync(cancellationToken);

        // Post-commit best-effort: Notifications
        await SendNotificationAsync(
            settlement.PayerId,
            "settlement_payment_confirmed",
            "Payment Confirmed",
            $"Your Razorpay payment of {amount:C} has been confirmed.",
            settlementId,
            cancellationToken);

        if (settlement.Status == SettlementStatuses.Settled)
        {
            await SendNotificationAsync(
                settlement.PayerId,
                "settlement_completed",
                "Settlement Completed",
                "Your settlement has been fully paid and marked as complete.",
                settlementId,
                cancellationToken);

            await SendNotificationAsync(
                settlement.ReceiverId,
                "settlement_completed",
                "Settlement Completed",
                "A settlement you are owed has been fully paid.",
                settlementId,
                cancellationToken);
        }

        // Post-commit best-effort: SignalR
        await BroadcastSettlementEventAsync(settlement, "SettlementPaymentConfirmed", new
        {
            settlementPayment.Id,
            settlementPayment.SettlementId,
            settlementPayment.Amount,
            settlementPayment.Status,
            settlementPayment.Method,
            SettlementStatus = settlement.Status,
            settlement.PaidAmount
        }, cancellationToken);
    }

    /// <summary>
    /// Queues an audit record in the current DbContext.
    /// Must be called BEFORE SaveChangesAsync so the audit is persisted within the same transaction.
    /// </summary>
    private async Task CreateAuditAsync(
        Guid settlementPaymentId, string action,
        string? oldStatus = null, string? newStatus = null,
        string? metadataJson = null, CancellationToken ct = default)
    {
        await auditService.RecordAsync(
            settlementPaymentId, currentUser.UserId, action,
            oldStatus, newStatus, metadataJson, ct);
    }

    /// <summary>
    /// Sends a notification to the target user. Called AFTER transaction commit.
    /// Failures are swallowed to prevent notification errors from affecting business operations.
    /// </summary>
    private async Task SendNotificationAsync(
        Guid targetUserId, string type, string title, string message,
        Guid? referenceId = null, CancellationToken ct = default)
    {
        try
        {
            await notificationService.CreateAndSendAsync(
                targetUserId, type, title, message,
                referenceId, currentUser.UserId, ct);
        }
        catch (Exception)
        {
            // Notification delivery is best-effort after successful commit
        }
    }

    /// <summary>
    /// Broadcasts a settlement payment event via SignalR. Called AFTER transaction commit.
    /// </summary>
    private async Task BroadcastSettlementEventAsync(
        Settlement settlement, string eventName, object payload, CancellationToken ct = default)
    {
        try
        {
            await realtimeNotifier.SendPaymentAsync(settlement.PayerId, eventName, payload, ct);
            await realtimeNotifier.SendPaymentAsync(settlement.ReceiverId, eventName, payload, ct);

            if (settlement.GroupId.HasValue)
            {
                await realtimeNotifier.SendGroupAsync(settlement.GroupId.Value, "SettlementUpdated", payload, ct);
            }
        }
        catch (Exception)
        {
            // SignalR delivery is best-effort
        }
    }

    /// <summary>
    /// Loads a settlement by ID or throws NotFoundException.
    /// </summary>
    private async Task<Settlement> GetSettlementOrThrowAsync(Guid settlementId, CancellationToken ct)
    {
        return await context.Settlements
            .Include(s => s.Payer)
            .Include(s => s.Receiver)
            .FirstOrDefaultAsync(s => s.Id == settlementId, ct)
            ?? throw new NotFoundException("Settlement not found.");
    }

    /// <summary>
    /// Loads a settlement payment with its settlement or throws NotFoundException.
    /// </summary>
    private async Task<SettlementPayment> GetPaymentOrThrowAsync(Guid paymentId, CancellationToken ct)
    {
        return await context.SettlementPayments
            .Include(sp => sp.Settlement)
            .FirstOrDefaultAsync(sp => sp.Id == paymentId, ct)
            ?? throw new NotFoundException("Settlement payment not found.");
    }

    private async Task RecalculateSettlementInternalAsync(Settlement settlement, CancellationToken ct)
    {
        var paidAmount = await context.SettlementPayments
            .Where(sp => sp.SettlementId == settlement.Id && sp.Status == SettlementPaymentStatuses.Confirmed)
            .SumAsync(sp => sp.Amount, ct);

        settlement.PaidAmount = paidAmount;

        if (paidAmount >= settlement.Amount)
        {
            settlement.Status = SettlementStatuses.Settled;
            settlement.CompletedAt = DateTimeOffset.UtcNow;
        }
        else if (paidAmount > 0)
        {
            settlement.Status = SettlementStatuses.PartiallySettled;
            settlement.CompletedAt = null;
        }
        else
        {
            settlement.Status = SettlementStatuses.Pending;
            settlement.CompletedAt = null;
        }
    }

    // Note: UpiReferenceNumber field stores generic transaction references (UPI ref, bank UTR, Razorpay ID).
    // A future migration will rename this column to TransactionReference for clarity.
    private static SettlementPaymentResponse ToPaymentResponse(SettlementPayment payment) =>
        new(
            payment.Id,
            payment.SettlementId,
            payment.FromUserId,
            payment.ToUserId,
            payment.Amount,
            payment.Status,
            payment.Method,
            payment.ProofUrl,
            payment.UpiReferenceNumber,
            payment.Notes,
            payment.ConfirmedAt,
            payment.RejectionReason,
            payment.CreatedAt);

    private static SettlementResponse ToResponse(Settlement settlement) =>
        new(
            settlement.Id,
            settlement.GroupId,
            settlement.PayerId,
            $"{settlement.Payer?.FirstName} {settlement.Payer?.LastName}".Trim(),
            settlement.ReceiverId,
            $"{settlement.Receiver?.FirstName} {settlement.Receiver?.LastName}".Trim(),
            settlement.Amount,
            settlement.Status,
            settlement.CreatedAt);

    private sealed record MemberBalanceProjection(Guid UserId, string FirstName, string LastName, string Email);

    private sealed record BalanceBucket(Guid UserId, decimal Amount);
}
