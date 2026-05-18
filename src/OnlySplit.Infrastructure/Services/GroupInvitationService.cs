using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OnlySplit.Application.Features.GroupInvitation;
using OnlySplit.Application.Features.Notifications;
using OnlySplit.Application.Interfaces;
using OnlySplit.Infrastructure.Database;

namespace OnlySplit.Infrastructure.Services;

public sealed class GroupInvitationService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser

) : IGroupInvitation
{
    public async Task SendInvitationAsync(CreateGroupInvitationRequest request, CancellationToken cancellationToken = default)
{
    var currentUserId = currentUser.UserId;

    var group = await context.Groups
        .Include(x => x.Members)
        .FirstOrDefaultAsync(
            x => x.Id == request.GroupId,
            cancellationToken);

    if (group is null)
    {
        throw new Exception("Group not found");
    }

    var isMember = group.Members.Any(x => x.UserId == currentUserId);

    if (!isMember)
    {
        throw new Exception("You are not a member");
    }

    var alreadyMember = group.Members.Any(x => x.UserId == request.InvitedUserId);

    if (alreadyMember)
    {
        throw new Exception("User already in group");
    }

    var existingInvite = await context.GroupInvitations
        .AnyAsync(x =>
            x.GroupId == request.GroupId &&
            x.InvitedUserId == request.InvitedUserId &&
            x.Status == "Pending",
            cancellationToken);

    if (existingInvite)
    {
        throw new Exception("Invitation already sent");
    }

    var invitation = new GroupInvitation
    {
        Id = Guid.NewGuid(),
        GroupId = request.GroupId,
        InvitedBy = currentUserId,
        InvitedUserId = request.InvitedUserId,
        Status = "Pending",
        CreatedAt = DateTime.UtcNow
    };

    await context.GroupInvitations.AddAsync(
        invitation,
        cancellationToken);

    var notification = new Notification
    {
        Id = Guid.NewGuid(),
        UserId = request.InvitedUserId,
        Type = "group_invitation",
        Title = "New Group Invitation",
        Message = $"You were invited to join {group.Name}",
        Payload = JsonSerializer.Serialize(new
        {
            GroupId = group.Id,
            InvitationId = invitation.Id
        }),
        CreatedAt = DateTime.UtcNow
    };

    await context.Notifications.AddAsync(
        notification,
        cancellationToken);

    await context.SaveChangesAsync(cancellationToken);
}
}