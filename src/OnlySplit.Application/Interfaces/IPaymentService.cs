using OnlySplit.Application.Features.Payments;

namespace OnlySplit.Application.Interfaces;

public interface IPaymentService
{
    Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task VerifyAsync(VerifyPaymentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PaymentHistoryResponse>> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);
}
