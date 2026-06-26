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
}
