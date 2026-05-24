using Microsoft.Extensions.Options;
using OnlySplit.Domain.Exceptions;
using OnlySplit.Application.Interfaces;
using OnlySplit.Domain.Utils;
using Razorpay.Api;

namespace OnlySplit.Infrastructure.Payments;

public sealed class RazorpayService(IOptions<RazorpayOptions> options) : IRazorpayService
{
private readonly RazorpayOptions _options = options.Value;

public async Task<RazorpayOrderResult> CreateOrderAsync(Guid settlementId, decimal amount, CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(_options.KeyId) || string.IsNullOrWhiteSpace(_options.KeySecret))
    {
        throw new PaymentException("Razorpay credentials are not configured.");
    }
    Console.WriteLine(_options.KeyId);
    Console.WriteLine(_options.KeySecret);
    var client = new RazorpayClient(_options.KeyId, _options.KeySecret);
    var orderRequest = new Dictionary<string, object>
    {
        ["amount"] = MoneyMath.ToPaise(amount),
        ["currency"] = _options.Currency,
        ["receipt"] = settlementId.ToString("N"),
        ["payment_capture"] = 1,
        ["notes"] = new Dictionary<string, string>
        {
            ["settlement_id"] = settlementId.ToString()
        }
    };

    dynamic order = client.Order.Create(orderRequest);
    Console.WriteLine(order);
    string? orderId = Convert.ToString(order["id"]);
    if (string.IsNullOrWhiteSpace(orderId))
    {
        throw new PaymentException("Razorpay did not return an order id.");
    }

    return new RazorpayOrderResult(orderId, MoneyMath.Round(amount), _options.Currency, _options.KeyId);
}
}
