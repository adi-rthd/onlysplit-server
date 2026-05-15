using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlySplit.Shared.Responses;
using OnlySplit.Application.Features.Groups;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.API.Controllers;

[ApiController]
[Authorize]
[Route("api/groups")]
public sealed class GroupsController(IGroupService groupService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<GroupResponse>>> Create(CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var response = await groupService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<GroupResponse>.Ok(response, "Group created successfully."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GroupResponse>>>> GetMine(CancellationToken cancellationToken)
    {
        var response = await groupService.GetMineAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<GroupResponse>>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<GroupResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await groupService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<GroupResponse>.Ok(response));
    }

    [HttpPost("{id:guid}/invite")]
    public async Task<ActionResult<ApiResponse<GroupResponse>>> Invite(Guid id, InviteGroupRequest request, CancellationToken cancellationToken)
    {
        var response = await groupService.InviteAsync(id, request, cancellationToken);
        return Ok(ApiResponse<GroupResponse>.Ok(response, "Member invited successfully."));
    }

    [HttpPost("{id:guid}/join")]
    public async Task<ActionResult<ApiResponse<GroupResponse>>> Join(Guid id, JoinGroupRequest request, CancellationToken cancellationToken)
    {
        var response = await groupService.JoinAsync(id, request, cancellationToken);
        return Ok(ApiResponse<GroupResponse>.Ok(response, "Joined group successfully."));
    }

    [HttpDelete("{id:guid}/member/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        await groupService.RemoveMemberAsync(id, userId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Member removed successfully."));
    }
}
