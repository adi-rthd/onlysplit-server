using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlySplit.Application.Dashboard.Interfaces;

namespace OnlySplit.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/dashboard")]
public class DashboardController(
    IDashboardService dashboardService
) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        CancellationToken cancellationToken
    )
    {
        var result = await dashboardService
            .GetSummaryAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Dashboard summary fetched successfully.",
            data = result,
            errors = Array.Empty<string>()
        });
    }
}