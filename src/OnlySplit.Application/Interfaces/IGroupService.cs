using OnlySplit.Application.Features.Groups;

namespace OnlySplit.Application.Interfaces;

public interface IGroupService
{
    Task<GroupResponse> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GroupResponse>> GetMineAsync(CancellationToken cancellationToken = default);
    Task<GroupResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GroupResponse> InviteAsync(Guid id, InviteGroupRequest request, CancellationToken cancellationToken = default);
    Task<GroupResponse> JoinAsync(Guid id, JoinGroupRequest request, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteGroup (Guid groudId, CancellationToken cancellationToken = default);
    Task<GroupResponse> UpdateAsync(Guid id, UpdateGroupRequest request, CancellationToken cancellationToken = default);
}
