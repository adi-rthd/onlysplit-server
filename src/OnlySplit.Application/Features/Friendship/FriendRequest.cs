namespace OnlySplit.Application.Features.Friendships;

public sealed record CreateFriendRequest(
    Guid AddresseeId
);

public sealed record FriendRequestResponse(
    Guid Id,
    Guid RequesterId,
    string RequesterName,
    string Status,
    DateTime CreatedAt
);

public sealed record SentFriendRequestResponse(
    Guid Id,
    Guid AddresseeId,
    string AddresseeName,
    string Status,
    DateTime CreatedAt
);