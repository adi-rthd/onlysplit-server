using FluentValidation;

namespace OnlySplit.Application.Features.Payments;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(request => request.SettlementIds).NotEmpty();
    }
}

public sealed class VerifyPaymentRequestValidator : AbstractValidator<VerifyPaymentRequest>
{
    public VerifyPaymentRequestValidator()
    {
        RuleFor(request => request.RazorpayOrderId).NotEmpty().MaximumLength(255);
        RuleFor(request => request.RazorpayPaymentId).NotEmpty().MaximumLength(255);
        RuleFor(request => request.RazorpaySignature).NotEmpty().MaximumLength(1000);
    }
}
