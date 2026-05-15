using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.Infrastructure.Payments;

public sealed class PaymentVerificationService(IOptions<RazorpayOptions> options) : IPaymentVerificationService
{
    private readonly RazorpayOptions _options = options.Value;

    public bool VerifyCheckoutSignature(string orderId, string paymentId, string signature)
    {
        var payload = $"{orderId}|{paymentId}";
        return VerifySignature(payload, signature, _options.KeySecret);
    }

    public bool VerifyWebhookSignature(string payload, string signature) =>
        VerifySignature(payload, signature, _options.WebhookSecret);

    private static bool VerifySignature(string payload, string signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        var computedBytes = Encoding.UTF8.GetBytes(computed);
        var providedBytes = Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant());

        return providedBytes.Length == computedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(computedBytes, providedBytes);
    }
}
