using Microsoft.AspNetCore.Mvc;
using OnlySplit.Application.Features.GroupInvitation;
using OnlySplit.Application.Interfaces;
using OnlySplit.Shared.Responses;

namespace OnlySplit.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/group-invitations")]
public sealed class GroupInvitationController(
    IGroupInvitation invitationService
) : ControllerBase
{
    [HttpPost("invite")]
    public async Task<ActionResult<ApiResponse<string>>> Invite(
        CreateGroupInvitationRequest request,
        CancellationToken cancellationToken)
    {
        await invitationService.SendInvitationAsync(
            request,
            cancellationToken);

        return Ok(
            ApiResponse<string>.Ok(
                "Invitation sent successfully."
            )
        );
    }
}