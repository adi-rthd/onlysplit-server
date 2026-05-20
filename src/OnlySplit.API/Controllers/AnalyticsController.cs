using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlySplit.Application.Analytics.Interfaces;
using OnlySplit.Shared.Responses;

namespace OnlySplit.API.Controllers;

[ApiController]
[Authorize]
[Route("api/analytics")]
public sealed class AnalyticsController(
    IAnalyticsService analyticsService
) : ControllerBase
{
    // =========================
    // SPENDING TRENDS
    // =========================
    [HttpGet("spending-trends")]
    public async Task<
        ActionResult<
            ApiResponse<object>
        >
    > GetSpendingTrends(
        CancellationToken cancellationToken
    )
    {
        var response =
            await analyticsService
                .GetSpendingTrendsAsync(
                    cancellationToken
                );

        return Ok(
            ApiResponse<object>.Ok(
                response,
                "Spending trends fetched successfully."
            )
        );
    }

    // =========================
    // CATEGORY BREAKDOWN
    // =========================
    [HttpGet("category-breakdown")]
    public async Task<
        ActionResult<
            ApiResponse<object>
        >
    > GetCategoryBreakdown(
        CancellationToken cancellationToken
    )
    {
        var response =
            await analyticsService
                .GetCategoryBreakdownAsync(
                    cancellationToken
                );

        return Ok(
            ApiResponse<object>.Ok(
                response,
                "Category breakdown fetched successfully."
            )
        );
    }

    // =========================
    // GROUP BREAKDOWN
    // =========================
    [HttpGet("group-breakdown")]
    public async Task<
        ActionResult<
            ApiResponse<object>
        >
    > GetGroupBreakdown(
        CancellationToken cancellationToken
    )
    {
        var response =
            await analyticsService
                .GetGroupBreakdownAsync(
                    cancellationToken
                );

        return Ok(
            ApiResponse<object>.Ok(
                response,
                "Group breakdown fetched successfully."
            )
        );
    }
}