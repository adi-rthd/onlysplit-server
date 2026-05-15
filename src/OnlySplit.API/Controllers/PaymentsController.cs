using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlySplit.Shared.Responses;
using OnlySplit.Application.Features.Payments;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.API.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpPost("create-order")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CreateOrderResponse>>> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var response = await paymentService.CreateOrderAsync(request, cancellationToken);
        return Ok(ApiResponse<CreateOrderResponse>.Ok(response, "Razorpay order created successfully."));
    }

    [HttpPost("verify")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Verify(VerifyPaymentRequest request, CancellationToken cancellationToken)
    {
        await paymentService.VerifyAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Payment verified successfully."));
    }

    [HttpGet("history")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PaymentHistoryResponse>>>> History(CancellationToken cancellationToken)
    {
        var response = await paymentService.GetHistoryAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<PaymentHistoryResponse>>.Ok(response));
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Webhook(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        await paymentService.HandleWebhookAsync(payload, signature, cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "Webhook processed successfully."));
    }
}
