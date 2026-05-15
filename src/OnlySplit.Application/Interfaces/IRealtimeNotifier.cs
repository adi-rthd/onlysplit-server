namespace OnlySplit.Application.Interfaces;

public interface IRealtimeNotifier
{
    Task SendActivityAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken = default);
    Task SendGroupAsync(Guid groupId, string eventName, object payload, CancellationToken cancellationToken = default);
    Task SendPaymentAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken = default);
}
