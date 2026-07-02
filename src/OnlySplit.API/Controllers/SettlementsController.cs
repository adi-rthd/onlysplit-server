using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlySplit.Shared.Responses;
using OnlySplit.Application.Features.Settlements;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.API.Controllers;

[ApiController]
[Authorize]
[Route("api/settlements")]
public sealed class SettlementsController(ISettlementService settlementService) : ControllerBase
{
    [HttpGet("group/{groupId:guid}/balances")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<BalanceResponse>>>> GetBalances(Guid groupId, CancellationToken cancellationToken)
    {
        var response = await settlementService.GetBalancesAsync(groupId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<BalanceResponse>>.Ok(response));
    }

    [HttpGet("group/{groupId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SettlementResponse>>>> GetPending(Guid groupId, CancellationToken cancellationToken)
    {
        var response = await settlementService.GetPendingSettlementsAsync(groupId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<SettlementResponse>>.Ok(response));
    }

    [HttpPost("group/{groupId:guid}/regenerate")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SettlementResponse>>>> Regenerate(Guid groupId, CancellationToken cancellationToken)
    {
        await settlementService.RegenerateForGroupAsync(groupId, cancellationToken);
        var response = await settlementService.GetPendingSettlementsAsync(groupId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<SettlementResponse>>.Ok(response, "Settlements regenerated successfully."));
    }
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SettlementResponse>>>> GetAllPending(CancellationToken cancellationToken)
    {
        var response = await settlementService.GetAllPendingSettlementsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<SettlementResponse>>.Ok(response));
    }
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SettlementResponse>>>> GetSummary(CancellationToken cancellationToken)
    {
        var response = await settlementService.GetSettlementSummaryAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<SettlementResponse>>.Ok(response));
    }

    /// <summary>
    /// Records a manual payment against a settlement.
    /// </summary>
    [HttpPost("{settlementId:guid}/payments")]
    public async Task<ActionResult<ApiResponse<SettlementPaymentResponse>>> RecordPayment(
        Guid settlementId, RecordManualPaymentRequest request, CancellationToken cancellationToken)
    {
        var response = await settlementService.RecordManualPaymentAsync(settlementId, request, cancellationToken);
        return Ok(ApiResponse<SettlementPaymentResponse>.Ok(response, "Payment recorded successfully."));
    }

    /// <summary>
    /// Returns the payment history for a settlement.
    /// </summary>
    [HttpGet("{settlementId:guid}/payments")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SettlementPaymentHistoryItem>>>> GetPaymentHistory(
        Guid settlementId, CancellationToken cancellationToken)
    {
        var response = await settlementService.GetPaymentHistoryAsync(settlementId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<SettlementPaymentHistoryItem>>.Ok(response));
    }

    /// <summary>
    /// Confirms a pending settlement payment (receiver only).
    /// </summary>
    [HttpPost("payments/{paymentId:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<SettlementPaymentResponse>>> ConfirmPayment(
        Guid paymentId, CancellationToken cancellationToken)
    {
        var response = await settlementService.ConfirmPaymentAsync(paymentId, cancellationToken);
        return Ok(ApiResponse<SettlementPaymentResponse>.Ok(response, "Payment confirmed successfully."));
    }

    /// <summary>
    /// Rejects a pending settlement payment (receiver only).
    /// </summary>
    [HttpPost("payments/{paymentId:guid}/reject")]
    public async Task<ActionResult<ApiResponse<SettlementPaymentResponse>>> RejectPayment(
        Guid paymentId, RejectPaymentRequest request, CancellationToken cancellationToken)
    {
        var response = await settlementService.RejectPaymentAsync(paymentId, request, cancellationToken);
        return Ok(ApiResponse<SettlementPaymentResponse>.Ok(response, "Payment rejected."));
    }

    /// <summary>
    /// Cancels a pending settlement payment (payer only).
    /// </summary>
    [HttpPost("payments/{paymentId:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<SettlementPaymentResponse>>> CancelPayment(
        Guid paymentId, CancellationToken cancellationToken)
    {
        var response = await settlementService.CancelPaymentAsync(paymentId, cancellationToken);
        return Ok(ApiResponse<SettlementPaymentResponse>.Ok(response, "Payment cancelled."));
    }

    /// <summary>
    /// Uploads proof of payment for a pending settlement payment (payer only).
    /// </summary>
    [HttpPost("payments/{paymentId:guid}/proof")]
    public async Task<ActionResult<ApiResponse<ProofUploadResponse>>> UploadProof(
        Guid paymentId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var response = await settlementService.UploadProofAsync(
            paymentId, stream, file.FileName, file.ContentType, file.Length, cancellationToken);
        return Ok(ApiResponse<ProofUploadResponse>.Ok(response, "Proof uploaded successfully."));
    }
}
