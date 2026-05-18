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

    [HttpGet("mine")]
    public async Task<ActionResult<
        ApiResponse<IReadOnlyCollection<GroupInvitationResponse>>>>
        Mine(CancellationToken cancellationToken)
    {
        var response = await invitationService
            .GetMyInvitationsAsync(cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyCollection<GroupInvitationResponse>>
                .Ok(response)
        );
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<ApiResponse<string>>> Accept(
        Guid id,
        CancellationToken cancellationToken)
    {
        await invitationService.AcceptInvitationAsync(
            id,
            cancellationToken);

        return Ok(
            ApiResponse<string>.Ok(
                "Invitation accepted."
            )
        );
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<string>>> Reject(
        Guid id,
        CancellationToken cancellationToken)
    {
        await invitationService.RejectInvitationAsync(id, cancellationToken);

        return Ok(
            ApiResponse<string>.Ok(
                "Invitation rejected."
            )
        );
    }
}