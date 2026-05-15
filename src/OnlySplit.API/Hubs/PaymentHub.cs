using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.API.Hubs;

[Authorize]
public sealed class PaymentHub(ICurrentUserService currentUser) : Hub
{
    public static string UserChannel(Guid userId) => $"payment:user:{userId:N}";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, UserChannel(currentUser.UserId));
        await base.OnConnectedAsync();
    }
}
