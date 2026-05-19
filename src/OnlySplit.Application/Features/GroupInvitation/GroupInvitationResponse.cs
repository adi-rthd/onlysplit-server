namespace OnlySplit.Application.Features.GroupInvitation;

public sealed record GroupInvitationResponse(
    Guid InvitationId,
    Guid GroupId,
    string GroupName,
    Guid InvitedBy,
    string InvitedByName,
    string Status,
    DateTime CreatedAt
);


public sealed record GetGroupInvitationResponse(
    Guid InvitationId,
    Guid GroupId,
    string GroupName,

    Guid InvitedUserId,
    string InvitedUserName,
    string InvitedUserEmail,

    Guid InvitedBy,
    string InvitedByName,

    string Status,
    DateTime CreatedAt
);