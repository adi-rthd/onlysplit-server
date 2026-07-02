namespace OnlySplit.Application.Features.Payments;

public class CreateOrderRequest
{
    // Change this from 'Guid SettlementId' to:
    public List<Guid> SettlementIds { get; set; } = new();
}
public sealed record CreateOrderResponse(
    Guid PaymentId,
    Guid SettlementId,
    string RazorpayOrderId,
    decimal Amount,
    string Currency,
    string KeyId);

public sealed record VerifyPaymentRequest(
    Guid? PaymentId,
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature);

public sealed record PaymentHistoryResponse(
    Guid Id,
    Guid SettlementId,
    string RazorpayOrderId,
    string? RazorpayPaymentId,
    decimal Amount,
    string Status,
    DateTimeOffset CreatedAt);
