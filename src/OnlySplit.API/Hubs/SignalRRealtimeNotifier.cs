using Microsoft.AspNetCore.SignalR;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.API.Hubs;

public sealed class SignalRRealtimeNotifier(
    IHubContext<ActivityHub> activityHub,
    IHubContext<GroupHub> groupHub,
    IHubContext<PaymentHub> paymentHub) : IRealtimeNotifier
{
    public Task SendActivityAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken = default) =>
        activityHub.Clients
            .Group(ActivityHub.UserChannel(userId))
            .SendAsync(eventName, payload, cancellationToken);

    public Task SendGroupAsync(Guid groupId, string eventName, object payload, CancellationToken cancellationToken = default) =>
        groupHub.Clients
            .Group(GroupHub.GroupChannel(groupId))
            .SendAsync(eventName, payload, cancellationToken);

    public Task SendPaymentAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken = default) =>
        paymentHub.Clients
            .Group(PaymentHub.UserChannel(userId))
            .SendAsync(eventName, payload, cancellationToken);
}
