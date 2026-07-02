using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OnlySplit.Domain.Constants;
using OnlySplit.Infrastructure.Database;
using OnlySplit.Application.Features.Payments;
using OnlySplit.Domain.Entities;
using OnlySplit.Domain.Exceptions;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.Infrastructure.Services;

public sealed class PaymentService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser,
    IRazorpayService razorpayService,
    IPaymentVerificationService paymentVerificationService,
    IActivityService activityService,
    IRealtimeNotifier realtimeNotifier,
    ISettlementService settlementService) : IPaymentService
{
    public async Task<CreateOrderResponse> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var settlementId = request.SettlementIds.First();

        var settlement = await context.Settlements
            .Include(candidate => candidate.Payer)
            .Include(candidate => candidate.Receiver)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == settlementId,
                cancellationToken)
            ?? throw new NotFoundException(
                "Settlement was not found.");

        if (settlement.PayerId != currentUser.UserId)
        {
            throw new ForbiddenException("Only the settlement payer can create this payment order.");
        }

        if (settlement.Status != SettlementStatuses.Pending)
        {
            throw new ConflictException("Only pending settlements can be paid.");
        }

        var razorpayOrder = await razorpayService.CreateOrderAsync(settlement.Id, settlement.Amount, cancellationToken);

        var payment = new Payment
        {
            SettlementId = settlement.Id,
            RazorpayOrderId = razorpayOrder.OrderId,
            Amount = settlement.Amount
        };

        context.Payments.Add(payment);

        await context.SaveChangesAsync(cancellationToken);

        await activityService.LogAsync(currentUser.UserId, ActivityTypes.PaymentCreated, new
        {
            payment.Id,
            payment.SettlementId,
            payment.Amount,
            payment.RazorpayOrderId
        }, cancellationToken);

        return new CreateOrderResponse(
            payment.Id,
            settlement.Id,
            razorpayOrder.OrderId,
            razorpayOrder.Amount,
            razorpayOrder.Currency,
            razorpayOrder.KeyId);
    }
    public async Task VerifyAsync(VerifyPaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (!paymentVerificationService.VerifyCheckoutSignature(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature))
        {
            throw new PaymentException("Razorpay payment signature verification failed.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var payment = await LoadPaymentForVerificationAsync(request.PaymentId, request.RazorpayOrderId, cancellationToken);
        var settlement = payment.Settlement!;

        if (settlement.PayerId != currentUser.UserId)
        {
            throw new ForbiddenException("Only the settlement payer can verify this payment.");
        }

        // Idempotency: if already completed, return success
        if (payment.Status == PaymentStatuses.Completed)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        if (settlement.Status is SettlementStatuses.Settled or SettlementStatuses.Cancelled)
        {
            throw new ConflictException("Settlement is no longer payable.");
        }

        // Update the gateway Payment record
        payment.RazorpayPaymentId = request.RazorpayPaymentId;
        payment.RazorpaySignature = request.RazorpaySignature;
        payment.Status = PaymentStatuses.Completed;
        await context.SaveChangesAsync(cancellationToken); // Save Payment status

        // Delegate settlement lifecycle to shared pipeline
        await settlementService.CreateSettlementPaymentForRazorpayAsync(
            settlement.Id, payment.Amount, request.RazorpayPaymentId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PaymentHistoryResponse>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        var payments = await context.Payments
            .AsNoTracking()
            .Include(payment => payment.Settlement)
            .Where(payment => payment.Settlement!.PayerId == userId || payment.Settlement.ReceiverId == userId)
            .OrderByDescending(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(ToHistoryResponse).ToArray();
    }

    public async Task HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        if (!paymentVerificationService.VerifyWebhookSignature(payload, signature))
        {
            throw new PaymentException("Razorpay webhook signature verification failed.");
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventName = root.TryGetProperty("event", out var eventProperty)
            ? eventProperty.GetString()
            : null;

        var orderId = TryGetString(root, "payload", "payment", "entity", "order_id");
        var paymentId = TryGetString(root, "payload", "payment", "entity", "id")
            ?? TryGetString(root, "payload", "refund", "entity", "payment_id");

        if (string.IsNullOrWhiteSpace(orderId) && string.IsNullOrWhiteSpace(paymentId))
        {
            return;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var payment = await context.Payments
            .Include(candidate => candidate.Settlement)
            .FirstOrDefaultAsync(candidate =>
                (!string.IsNullOrWhiteSpace(orderId) && candidate.RazorpayOrderId == orderId) ||
                (!string.IsNullOrWhiteSpace(paymentId) && candidate.RazorpayPaymentId == paymentId),
                cancellationToken);

        if (payment?.Settlement is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        if (eventName == "payment.captured")
        {
            payment.RazorpayPaymentId ??= paymentId;

            // Idempotency: skip if already completed
            if (payment.Status == PaymentStatuses.Completed)
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            payment.Status = PaymentStatuses.Completed;
            await context.SaveChangesAsync(cancellationToken); // Save Payment status first

            // Delegate settlement lifecycle to shared pipeline (handles its own SaveChanges)
            await settlementService.CreateSettlementPaymentForRazorpayAsync(
                payment.Settlement.Id, payment.Amount, payment.RazorpayPaymentId!, cancellationToken);
        }
        else if (eventName == "payment.failed")
        {
            payment.RazorpayPaymentId ??= paymentId;
            payment.Status = PaymentStatuses.Failed;
            await context.SaveChangesAsync(cancellationToken);
        }
        else if (eventName is "refund.created" or "refund.processed")
        {
            payment.Status = PaymentStatuses.Refunded;
            // TODO: Implement refund pipeline to reverse SettlementPayment and recalculate via shared pipeline
            payment.Settlement.Status = SettlementStatuses.Pending;
            await context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        // Post-commit notifications (best-effort) — captured events handled by shared pipeline
        if (payment.Status == PaymentStatuses.Failed)
        {
            await NotifyPaymentStatusAsync(payment.Id, "PaymentFailed", ActivityTypes.PaymentFailed, cancellationToken);
        }
        else if (payment.Status == PaymentStatuses.Refunded)
        {
            await NotifyPaymentStatusAsync(payment.Id, "PaymentRefunded", ActivityTypes.PaymentRefunded, cancellationToken);
        }
    }

    private async Task<Payment> LoadPaymentForVerificationAsync(Guid? paymentId, string razorpayOrderId, CancellationToken cancellationToken)
    {
        var query = context.Payments.Include(payment => payment.Settlement).AsQueryable();

        var payment = paymentId.HasValue
            ? await query.FirstOrDefaultAsync(candidate => candidate.Id == paymentId.Value, cancellationToken)
            : await query.FirstOrDefaultAsync(candidate => candidate.RazorpayOrderId == razorpayOrderId, cancellationToken);

        if (payment is null)
        {
            throw new NotFoundException("Payment order was not found.");
        }

        if (payment.RazorpayOrderId != razorpayOrderId)
        {
            throw new PaymentException("Payment order id does not match the stored Razorpay order.");
        }

        return payment;
    }

    private async Task NotifyPaymentCompletedAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        await NotifyPaymentStatusAsync(paymentId, "PaymentCompleted", ActivityTypes.PaymentCompleted, cancellationToken);
    }

    private async Task NotifyPaymentStatusAsync(Guid paymentId, string eventName, string activityType, CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .AsNoTracking()
            .Include(candidate => candidate.Settlement)
            .FirstAsync(candidate => candidate.Id == paymentId, cancellationToken);

        var settlement = payment.Settlement!;
        var payload = new
        {
            payment.Id,
            payment.SettlementId,
            payment.RazorpayOrderId,
            payment.RazorpayPaymentId,
            payment.Amount,
            payment.Status,
            SettlementStatus = settlement.Status,
            settlement.GroupId
        };

        await activityService.LogAsync(settlement.PayerId, activityType, payload, cancellationToken);

        await realtimeNotifier.SendPaymentAsync(settlement.PayerId, eventName, payload, cancellationToken);
        await realtimeNotifier.SendPaymentAsync(settlement.ReceiverId, eventName, payload, cancellationToken);

        if (settlement.GroupId.HasValue)
        {
            await realtimeNotifier.SendGroupAsync(settlement.GroupId.Value, "SettlementUpdated", payload, cancellationToken);
        }
    }

    private static string? TryGetString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var part in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    private static PaymentHistoryResponse ToHistoryResponse(Payment payment) =>
        new(
            payment.Id,
            payment.SettlementId,
            payment.RazorpayOrderId,
            payment.RazorpayPaymentId,
            payment.Amount,
            payment.Status,
            payment.CreatedAt);
}
