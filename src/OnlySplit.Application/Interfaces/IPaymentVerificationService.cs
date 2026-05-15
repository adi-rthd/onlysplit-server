namespace OnlySplit.Application.Interfaces;

public interface IPaymentVerificationService
{
    bool VerifyCheckoutSignature(string orderId, string paymentId, string signature);
    bool VerifyWebhookSignature(string payload, string signature);
}
