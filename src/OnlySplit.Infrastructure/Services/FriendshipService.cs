using Microsoft.EntityFrameworkCore;
using OnlySplit.Application.Features.Friendships;
using OnlySplit.Application.Interfaces;
using OnlySplit.Domain.Entities;
using OnlySplit.Domain.Exceptions;
using OnlySplit.Infrastructure.Database;

namespace OnlySplit.Infrastructure.Services;

public sealed class FriendshipService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser
) : IFriendshipService
{
    public async Task SendRequestAsync(
        CreateFriendRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        if (userId == request.AddresseeId)
        {
            throw new ConflictException(
                "You cannot send request to yourself.");
        }

        var existing = await context.Friendships
            .AnyAsync(
                x =>
                    (
                        x.RequesterId == userId &&
                        x.AddresseeId == request.AddresseeId
                    )
                    ||
                    (
                        x.RequesterId == request.AddresseeId &&
                        x.AddresseeId == userId
                    ),
                cancellationToken);

        if (existing)
        {
            throw new ConflictException(
                "Friend request already exists.");
        }

        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            RequesterId = userId,
            AddresseeId = request.AddresseeId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await context.Friendships.AddAsync(
            friendship,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FriendRequestResponse>>
        GetRequestsAsync(
            CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        return await context.Friendships
            .AsNoTracking()
            .Where(x =>
                x.AddresseeId == userId &&
                x.Status == "Pending")
            .Include(x => x.Requester)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new FriendRequestResponse(
                x.Id,
                x.RequesterId,
                $"{x.Requester.FirstName} {x.Requester.LastName}",
                x.Status,
                x.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task AcceptRequestAsync(
        Guid friendshipId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        var friendship = await context.Friendships
            .FirstOrDefaultAsync(
                x =>
                    x.Id == friendshipId &&
                    x.AddresseeId == userId,
                cancellationToken);

        if (friendship is null)
        {
            throw new NotFoundException(
                "Friend request not found.");
        }

        if (friendship.Status != "Pending")
        {
            throw new ConflictException(
                "Request already processed.");
        }

        friendship.Status = "Accepted";

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectRequestAsync(
        Guid friendshipId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        var friendship = await context.Friendships
            .FirstOrDefaultAsync(
                x =>
                    x.Id == friendshipId &&
                    x.AddresseeId == userId,
                cancellationToken);

        if (friendship is null)
        {
            throw new NotFoundException(
                "Friend request not found.");
        }

        if (friendship.Status != "Pending")
        {
            throw new ConflictException(
                "Request already processed.");
        }

        friendship.Status = "Rejected";

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FriendResponse>>
        GetFriendsAsync(
            CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        var friendships = await context.Friendships
            .AsNoTracking()
            .Include(x => x.Requester)
            .Include(x => x.Addressee)
            .Where(x =>
                (
                    x.RequesterId == userId ||
                    x.AddresseeId == userId
                )
                &&
                x.Status == "Accepted")
            .ToListAsync(cancellationToken);

        return friendships
            .Select(x =>
            {
                var friend =
                    x.RequesterId == userId
                        ? x.Addressee
                        : x.Requester;

                return new FriendResponse(
                    friend.Id,
                    friend.FirstName,
                    friend.LastName,
                    friend.Email,
                    friend.AvatarUrl
                );
            })
            .ToList();
    }

    public async Task RemoveFriendAsync(
        Guid friendId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        var friendship = await context.Friendships
            .FirstOrDefaultAsync(
                x =>
                    (
                        x.RequesterId == userId &&
                        x.AddresseeId == friendId
                    )
                    ||
                    (
                        x.RequesterId == friendId &&
                        x.AddresseeId == userId
                    ),
                cancellationToken);

        if (friendship is null)
        {
            throw new NotFoundException("Friendship not found.");
        }

        context.Friendships.Remove(friendship);

        await context.SaveChangesAsync(cancellationToken);
    }
}