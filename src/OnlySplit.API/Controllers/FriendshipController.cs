using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlySplit.Application.Features.Auth;
using OnlySplit.Application.Features.Friendships;
using OnlySplit.Application.Interfaces;
using OnlySplit.Shared.Responses;

namespace OnlySplit.API.Controllers;

[ApiController]
[Authorize]
[Route("api/friends")]
public sealed class FriendshipController(
    IFriendshipService friendshipService
) : ControllerBase
{
    [HttpPost("request")]
    public async Task<ActionResult<ApiResponse<string>>>
        SendRequest(
            CreateFriendRequest request,
            CancellationToken cancellationToken)
    {
        await friendshipService.SendRequestAsync(
            request,
            cancellationToken);

        return Ok(
            ApiResponse<string>.Ok(
                "Friend request sent."
            )
        );
    }

    [HttpGet("requests")]
    public async Task<ActionResult<
        ApiResponse<IReadOnlyCollection<FriendRequestResponse>>>>
        Requests(CancellationToken cancellationToken)
    {
        var response = await friendshipService
            .GetRequestsAsync(cancellationToken);

        return Ok(
            ApiResponse<
                IReadOnlyCollection<FriendRequestResponse>>
                .Ok(response)
        );
    }

    [HttpGet("sent")]
    public async Task<ActionResult<
        ApiResponse<IReadOnlyCollection<SentFriendRequestResponse>>>>
        Sent(CancellationToken cancellationToken)
    {
        var response = await friendshipService
            .GetSentRequestsAsync(cancellationToken);

        return Ok(
            ApiResponse<
                IReadOnlyCollection<SentFriendRequestResponse>>
                .Ok(response)
        );
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<ApiResponse<string>>>
        Accept(
            Guid id,
            CancellationToken cancellationToken)
    {
        await friendshipService.AcceptRequestAsync(
            id,
            cancellationToken);

        return Ok(
            ApiResponse<string>.Ok(
                "Friend request accepted."
            )
        );
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<string>>>
        Reject(
            Guid id,
            CancellationToken cancellationToken)
    {
        await friendshipService.RejectRequestAsync(
            id,
            cancellationToken);

        return Ok(
            ApiResponse<string>.Ok(
                "Friend request rejected."
            )
        );
    }

    [HttpGet]
    public async Task<ActionResult<
        ApiResponse<IReadOnlyCollection<FriendResponse>>>>
        Friends(CancellationToken cancellationToken)
    {
        var response = await friendshipService
            .GetFriendsAsync(cancellationToken);

        return Ok(
            ApiResponse<
                IReadOnlyCollection<FriendResponse>>
                .Ok(response)
        );
    }

    [HttpDelete("{friendId:guid}")]
    public async Task<ActionResult<ApiResponse<string>>>
        Remove(
            Guid friendId,
            CancellationToken cancellationToken)
    {
        await friendshipService.RemoveFriendAsync(
            friendId,
            cancellationToken);

        return Ok(
            ApiResponse<string>.Ok(
                "Friend removed."
            )
        );
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserSearchResponse>>>> Search(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        var response = await friendshipService.SearchUsersAsync(q, cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<UserSearchResponse>>
            .Ok(response));
    }
}