namespace OnlySplit.Application.Features.Friendships;

public sealed record FriendResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? AvatarUrl
);