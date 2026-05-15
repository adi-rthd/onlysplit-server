using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.API.Hubs;

[Authorize]
public sealed class GroupHub(IGroupMembershipReader groupMembershipReader, ICurrentUserService currentUser) : Hub
{
    public static string GroupChannel(Guid groupId) => $"group:{groupId:N}";

    public override async Task OnConnectedAsync()
    {
        var groupIds = await groupMembershipReader.GetGroupIdsForUserAsync(currentUser.UserId, Context.ConnectionAborted);

        foreach (var groupId in groupIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupChannel(groupId), Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinGroup(Guid groupId)
    {
        var isMember = await groupMembershipReader.IsGroupMemberAsync(groupId, currentUser.UserId, Context.ConnectionAborted);

        if (isMember)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupChannel(groupId), Context.ConnectionAborted);
        }
    }

    public Task LeaveGroup(Guid groupId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupChannel(groupId), Context.ConnectionAborted);
}
