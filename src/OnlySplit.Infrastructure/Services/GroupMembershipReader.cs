using Microsoft.EntityFrameworkCore;
using OnlySplit.Application.Interfaces;
using OnlySplit.Infrastructure.Database;

namespace OnlySplit.Infrastructure.Services;

public sealed class GroupMembershipReader(OnlySplitDbContext context) : IGroupMembershipReader
{
    public async Task<IReadOnlyCollection<Guid>> GetGroupIdsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.GroupMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId)
            .Select(member => member.GroupId)
            .ToArrayAsync(cancellationToken);

    public Task<bool> IsGroupMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default) =>
        context.GroupMembers
            .AsNoTracking()
            .AnyAsync(member => member.GroupId == groupId && member.UserId == userId, cancellationToken);
}
