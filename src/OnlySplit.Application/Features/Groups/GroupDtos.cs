namespace OnlySplit.Application.Features.Groups;

public sealed record CreateGroupRequest(string Name, string description);

public sealed record InviteGroupRequest(string Email);

public sealed record JoinGroupRequest(string InviteCode);

public sealed record GroupMemberResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? AvatarUrl,
    DateTimeOffset JoinedAt);

public record GroupResponse(
    Guid Id,
    string Name,
    Guid CreatedBy,
    string Description,
    string Currency,
    DateTimeOffset CreatedAt,
    string InviteCode,
    decimal TotalSpending,
    IReadOnlyCollection<GroupMemberResponse> Members
);

public sealed record UpdateGroupRequest(
    string? Name, 
    string? Description, 
    string? Currency
);
