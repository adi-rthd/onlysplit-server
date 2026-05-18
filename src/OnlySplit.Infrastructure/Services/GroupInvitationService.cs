    using System.Text.Json;
    using OnlySplit.Domain.Exceptions;
    using Microsoft.EntityFrameworkCore;
    using OnlySplit.Application.Features.GroupInvitation;
    using OnlySplit.Application.Features.Notifications;
    using OnlySplit.Application.Interfaces;
    using OnlySplit.Domain.Entities;
    using OnlySplit.Infrastructure.Database;
    namespace OnlySplit.Infrastructure.Services;

    public sealed class GroupInvitationService(
        OnlySplitDbContext context,
        ICurrentUserService currentUser) : IGroupInvitation
    {

        public async Task SendInvitationAsync(
            CreateGroupInvitationRequest request,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = currentUser.UserId;

            var group = await context.Groups
                .Include(x => x.Members)
                .FirstOrDefaultAsync(
                    x => x.Id == request.GroupId,
                    cancellationToken);

            if (group is null)
            {
                throw new NotFoundException("Group not found.");
            }

            var isMember = group.Members.Any(x => x.UserId == currentUserId);

            if (!isMember)
            {
                throw new ForbiddenException("You are not a member of this group.");
            }

            var alreadyMember = group.Members
                .Any(x => x.UserId == request.InvitedUserId);

            if (alreadyMember)
            {
                throw new ConflictException("User is already in the group.");
            }

            var existingInvite = await context.GroupInvitations
                .AnyAsync(
                    x =>
                        x.GroupId == request.GroupId &&
                        x.InvitedUserId == request.InvitedUserId &&
                        x.Status == "Pending",
                    cancellationToken);

            if (existingInvite)
            {
                throw new ConflictException("Invitation already sent.");
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
        public async Task<IReadOnlyCollection<GroupInvitationResponse>>
        GetMyInvitationsAsync(CancellationToken cancellationToken = default)
        {
            var userId = currentUser.UserId;

            var invitations = await context.GroupInvitations
                .AsNoTracking()
                .Where(x =>
                    x.InvitedUserId == userId &&
                    x.Status == "Pending")
                .Include(x => x.Group)
                .Include(x => x.InvitedByUser)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new GroupInvitationResponse(
                    x.Id,
                    x.GroupId,
                    x.Group.Name,
                    x.InvitedBy,
                    $"{x.InvitedByUser.FirstName} {x.InvitedByUser.LastName}",
                    x.Status,
                    x.CreatedAt
                ))
                .ToListAsync(cancellationToken);

            return invitations;
        }

        public async Task AcceptInvitationAsync(
            Guid invitationId,
            CancellationToken cancellationToken = default)
        {
            var userId = currentUser.UserId;

            var invitation = await context.GroupInvitations
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == invitationId &&
                        x.InvitedUserId == userId,
                    cancellationToken);

            if (invitation is null)
            {
                throw new NotFoundException("Invitation not found.");
            }

            if (invitation.Status != "Pending")
            {
                throw new ConflictException("Invitation already processed.");
            }

            var alreadyMember = await context.GroupMembers
                .AnyAsync(
                    x =>
                        x.GroupId == invitation.GroupId &&
                        x.UserId == userId,
                    cancellationToken);

            if (alreadyMember)
            {
                throw new ConflictException("You are already a member of this group.");
            }

            var groupMember = new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = invitation.GroupId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };

            await context.GroupMembers.AddAsync(groupMember, cancellationToken);

            invitation.Status = "Accepted";

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = invitation.InvitedBy,
                Type = "group_invitation_accepted",
                Title = "Invitation Accepted",
                Message = "A user accepted your group invitation.",
                CreatedAt = DateTime.UtcNow
            };

            await context.Notifications.AddAsync(
                notification,
                cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task RejectInvitationAsync(
            Guid invitationId,
            CancellationToken cancellationToken = default)
        {
            var userId = currentUser.UserId;

            var invitation = await context.GroupInvitations.FirstOrDefaultAsync(x => x.Id == invitationId && x.InvitedUserId == userId, cancellationToken);

            if (invitation is null)
            {
                throw new NotFoundException("Invitation not found.");
            }

            if (invitation.Status != "Pending")
            {
                throw new ConflictException("Rejection already processed.");
            }

            invitation.Status = "Rejected";

            await context.SaveChangesAsync(cancellationToken);
        }
    }