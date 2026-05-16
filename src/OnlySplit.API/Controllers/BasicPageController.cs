using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlySplit.Application.Features.BasicPage;
using OnlySplit.Application.Interfaces;
using OnlySplit.Shared.Responses;

namespace OnlySplit.API.Controllers;

[ApiController]
[Route("api/basic-page")]
public sealed class BasicPageController(
IBasicPageService basicPageService
) : ControllerBase
{
    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LandingStatsResponse>>> GetStats(
        CancellationToken cancellationToken)
    {
        var response = await basicPageService.GetLandingStatsAsync(
            cancellationToken
        );

        return Ok(
            ApiResponse<LandingStatsResponse>.Ok(
                response,
                "Landing page stats fetched successfully."
            )
        );
    }
}