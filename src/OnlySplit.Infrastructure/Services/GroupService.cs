using Microsoft.EntityFrameworkCore;
using OnlySplit.Domain.Constants;
using OnlySplit.Infrastructure.Database;
using OnlySplit.Application.Features.Groups;
using OnlySplit.Domain.Entities;
using OnlySplit.Domain.Exceptions;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.Infrastructure.Services;

public sealed class GroupService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser,
    IActivityService activityService,
    IRealtimeNotifier realtimeNotifier) : IGroupService
{
    public async Task<GroupResponse> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        var group = new Group
        {
            Name = request.Name.Trim(),
            CreatedBy = userId
        };

        group.Members.Add(new GroupMember
        {
            GroupId = group.Id,
            UserId = userId
        });

        context.Groups.Add(group);
        await context.SaveChangesAsync(cancellationToken);

        await activityService.LogAsync(userId, ActivityTypes.GroupCreated, new { group.Id, group.Name }, cancellationToken);
        return await GetByIdAsync(group.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<GroupResponse>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        var groups = await context.Groups
          .AsNoTracking()
          .Include(group => group.Members)
              .ThenInclude(member => member.User)
          .Include(group => group.Expenses)
          .Where(group =>
              group.Members.Any(member =>
                  member.UserId == userId
              )
          )
          .OrderByDescending(group => group.CreatedAt)
          .ToListAsync(cancellationToken);

        return groups.Select(ToResponse).ToArray();
    }

    public async Task<GroupResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupAsync(id, tracking: false, cancellationToken);
        EnsureMember(group);
        return ToResponse(group);
    }

    public async Task<GroupResponse> InviteAsync(Guid id, InviteGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupAsync(id, tracking: true, cancellationToken);
        EnsureOwner(group);

        var email = request.Email.Trim().ToLowerInvariant();
        var invitedUser = await context.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken)
            ?? throw new NotFoundException("Invited user was not found.");

        var alreadyMember = group.Members.Any(member => member.UserId == invitedUser.Id);
        if (!alreadyMember)
        {
            group.Members.Add(new GroupMember { GroupId = group.Id, UserId = invitedUser.Id });
            await context.SaveChangesAsync(cancellationToken);
        }

        await activityService.LogAsync(currentUser.UserId, ActivityTypes.MemberJoined, new
        {
            GroupId = group.Id,
            group.Name,
            UserId = invitedUser.Id,
            invitedUser.Email
        }, cancellationToken);

        await realtimeNotifier.SendGroupAsync(group.Id, "MemberJoined", new { group.Id, UserId = invitedUser.Id, invitedUser.Email }, cancellationToken);

        return await GetByIdAsync(group.Id, cancellationToken);
    }

    public async Task<GroupResponse> JoinAsync(Guid id, JoinGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupAsync(id, tracking: true, cancellationToken);
        if (!string.Equals(group.InviteCode, request.InviteCode, StringComparison.Ordinal))
        {
            throw new ForbiddenException("Invite code is invalid.");
        }

        var userId = currentUser.UserId;
        if (group.Members.All(member => member.UserId != userId))
        {
            group.Members.Add(new GroupMember { GroupId = group.Id, UserId = userId });
            await context.SaveChangesAsync(cancellationToken);
        }

        await activityService.LogAsync(userId, ActivityTypes.MemberJoined, new { GroupId = group.Id, group.Name }, cancellationToken);
        await realtimeNotifier.SendGroupAsync(group.Id, "MemberJoined", new { group.Id, UserId = userId }, cancellationToken);

        return await GetByIdAsync(group.Id, cancellationToken);
    }

    public async Task RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupAsync(id, tracking: true, cancellationToken);
        EnsureOwner(group);

        if (userId == group.CreatedBy)
        {
            throw new ConflictException("Group owner cannot be removed.");
        }

        var member = group.Members.FirstOrDefault(candidate => candidate.UserId == userId)
            ?? throw new NotFoundException("Group member was not found.");

        context.GroupMembers.Remove(member);
        await context.SaveChangesAsync(cancellationToken);

        await activityService.LogAsync(currentUser.UserId, ActivityTypes.MemberRemoved, new { GroupId = group.Id, group.Name, UserId = userId }, cancellationToken);
        await realtimeNotifier.SendGroupAsync(group.Id, "MemberRemoved", new { group.Id, UserId = userId }, cancellationToken);
    }

    private async Task<Group> LoadGroupAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        var query = context.Groups
            .Include(group => group.Members)
                .ThenInclude(member => member.User)
            .Where(group => group.Id == id);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Group was not found.");
    }

    private void EnsureMember(Group group)
    {
        if (group.Members.All(member => member.UserId != currentUser.UserId))
        {
            throw new ForbiddenException("You are not a member of this group.");
        }
    }

    private void EnsureOwner(Group group)
    {
        if (group.CreatedBy != currentUser.UserId)
        {
            throw new ForbiddenException("Only the group owner can perform this action.");
        }
    }

    private static GroupResponse ToResponse(Group group) =>
       new(
           group.Id,
           group.Name,
           group.CreatedBy,
           group.Currency,
           group.CreatedAt,
           group.InviteCode,
           group.Expenses.Sum(
               expense => expense.Amount
           ),

           group.Members
               .OrderBy(member => member.JoinedAt)
               .Select(member => new GroupMemberResponse(
                   member.UserId,
                   member.User?.FirstName ?? string.Empty,
                   member.User?.LastName ?? string.Empty,
                   member.User?.Email ?? string.Empty,
                   member.User?.AvatarUrl,
                   member.JoinedAt
               ))
               .ToArray()
       );
}
