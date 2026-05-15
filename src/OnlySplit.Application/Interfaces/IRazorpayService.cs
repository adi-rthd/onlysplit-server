namespace OnlySplit.Application.Interfaces;

public interface IRazorpayService
{
    Task<RazorpayOrderResult> CreateOrderAsync(Guid settlementId, decimal amount, CancellationToken cancellationToken = default);
}

public sealed record RazorpayOrderResult(string OrderId, decimal Amount, string Currency, string KeyId);
