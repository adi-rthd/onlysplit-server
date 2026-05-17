using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlySplit.Application.Activities.Interfaces;

namespace OnlySplit.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/activity")]
public class ActivityController(
    IActivityFeedService activityFeedService
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActivities(
        [FromQuery] string scope = "groups",
        CancellationToken cancellationToken = default
    )
    {
        var result = await activityFeedService
            .GetActivitiesAsync(
                scope,
                cancellationToken
            );

        return Ok(new
        {
            success = true,
            message = "Activities fetched successfully.",
            data = result,
            errors = Array.Empty<string>()
        });
    }
}