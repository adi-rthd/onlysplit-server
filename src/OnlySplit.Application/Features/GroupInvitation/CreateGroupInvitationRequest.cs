namespace OnlySplit.Application.Features.GroupInvitation;

public sealed record CreateGroupInvitationRequest(
    Guid GroupId,
    Guid InvitedUserId
);