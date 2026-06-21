using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlySplit.Application.Features.Auth;
using OnlySplit.Application.Features.Friendships;
using OnlySplit.Application.Interfaces;
using OnlySplit.Domain.Entities;
using OnlySplit.Domain.Exceptions;
using OnlySplit.Infrastructure.Database;

namespace OnlySplit.Infrastructure.Services;

public sealed class FriendshipService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser,
    IRealtimeNotifier realtimeNotifier,
    ILogger<FriendshipService> logger
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

        var sender = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

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

        // Real-time notification to recipient
        try
        {
            await realtimeNotifier.SendActivityAsync(
                request.AddresseeId,
                "FriendRequestReceived",
                new
                {
                    FriendshipId = friendship.Id,
                    SenderId = userId,
                    SenderName = sender is not null
                        ? $"{sender.FirstName} {sender.LastName}".Trim()
                        : "Unknown"
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send FriendRequestReceived notification.");
        }
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

    public async Task<IReadOnlyCollection<SentFriendRequestResponse>>
        GetSentRequestsAsync(
            CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        return await context.Friendships
            .AsNoTracking()
            .Where(x =>
                x.RequesterId == userId &&
                x.Status == "Pending")
            .Include(x => x.Addressee)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new SentFriendRequestResponse(
                x.Id,
                x.AddresseeId,
                $"{x.Addressee.FirstName} {x.Addressee.LastName}",
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
            .Include(x => x.Addressee)
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

        // Notify the original sender
        try
        {
            await realtimeNotifier.SendActivityAsync(
                friendship.RequesterId,
                "FriendRequestAccepted",
                new
                {
                    FriendshipId = friendship.Id,
                    RecipientId = userId,
                    RecipientName = friendship.Addressee is not null
                        ? $"{friendship.Addressee.FirstName} {friendship.Addressee.LastName}".Trim()
                        : "Unknown"
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send FriendRequestAccepted notification.");
        }
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

        // Notify the original sender
        try
        {
            await realtimeNotifier.SendActivityAsync(
                friendship.RequesterId,
                "FriendRequestRejected",
                new
                {
                    FriendshipId = friendship.Id,
                    RecipientId = userId
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send FriendRequestRejected notification.");
        }
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

    public async Task<IReadOnlyCollection<UserSearchResponse>>
        SearchUsersAsync(
            string query,
            CancellationToken cancellationToken = default)
    {
        query = query.Trim().ToLower();

        var userId = currentUser.UserId;

        var friendshipUserIds = await context.Friendships
            .AsNoTracking()
            .Where(x =>
                x.RequesterId == userId || x.AddresseeId == userId)
            .Select(x => x.RequesterId == userId ? x.AddresseeId : x.RequesterId)
            .ToListAsync(cancellationToken);

        var excludeIds = friendshipUserIds.ToHashSet();
        excludeIds.Add(userId);

        return await context.Users
            .AsNoTracking()
            .Where(x =>
                !excludeIds.Contains(x.Id) &&
                (
                    x.Email.ToLower().Contains(query) ||
                    x.FirstName.ToLower().Contains(query) ||
                    x.LastName.ToLower().Contains(query)
                ))
            .OrderBy(x => x.FirstName)
            .Take(10)
            .Select(x => new UserSearchResponse(
                x.Id,
                x.FirstName,
                x.LastName,
                x.Email,
                x.AvatarUrl
            ))
            .ToListAsync(cancellationToken);
    }
}