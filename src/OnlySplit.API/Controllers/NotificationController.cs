using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlySplit.Application.Features.Notifications;
using OnlySplit.Application.Interfaces;
using OnlySplit.Shared.Responses;

namespace OnlySplit.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<NotificationResponse>>>> Get(CancellationToken cancellationToken)
    {
        var response = await notificationService.GetNotificationsAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<NotificationResponse>>.Ok(response));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<string>>> Read(
        Guid id,
        CancellationToken cancellationToken)
    {
        await notificationService.MarkAsReadAsync(id, cancellationToken);

        return Ok(ApiResponse<string>.Ok("Notification marked as read."));
    }
    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllAsReadAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "All notifications marked as read."));
    }
}