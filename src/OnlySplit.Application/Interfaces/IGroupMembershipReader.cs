namespace OnlySplit.Application.Interfaces;

public interface IGroupMembershipReader
{
    Task<IReadOnlyCollection<Guid>> GetGroupIdsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsGroupMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);
}
