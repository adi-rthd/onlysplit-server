using OnlySplit.Application.Features.Auth;
using OnlySplit.Application.Features.Friendships;

namespace OnlySplit.Application.Interfaces;

public interface IFriendshipService
{
    Task SendRequestAsync(
        CreateFriendRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FriendRequestResponse>>
        GetRequestsAsync(
            CancellationToken cancellationToken = default);

    Task AcceptRequestAsync(
        Guid friendshipId,
        CancellationToken cancellationToken = default);

    Task RejectRequestAsync(
        Guid friendshipId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FriendResponse>>
        GetFriendsAsync(
            CancellationToken cancellationToken = default);

    Task RemoveFriendAsync(
        Guid friendId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserSearchResponse>>
        SearchUsersAsync(
            string query,
            CancellationToken cancellationToken = default);
}