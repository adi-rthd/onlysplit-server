using Microsoft.EntityFrameworkCore;
using OnlySplit.Application.Activities.DTOs;
using OnlySplit.Application.Activities.Interfaces;
using OnlySplit.Application.Interfaces;
using OnlySplit.Infrastructure.Database;

namespace OnlySplit.Application.Activities.Services;

public class ActivityFeedService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser
) : IActivityFeedService
{
    public async Task<IReadOnlyCollection<ActivityResponse>>
        GetActivitiesAsync(string scope, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        var groupsQuery = context.Groups
            .AsNoTracking()
            .Include(group => group.Members)
            .Include(group => group.Expenses)
            .Where(group =>
                group.Members.Any(member =>
                    member.UserId == userId
                )
            );

        var groups = await groupsQuery
            .ToListAsync(cancellationToken);

        var activities = groups
            .SelectMany(group =>
                group.Expenses
                    .Where(expense =>
                        scope == "mine"
                            ? expense.PaidBy == userId
                            : true
                    )
                    .Select(expense =>
                        new ActivityResponse
                        {
                            Id = expense.Id,

                            Type = "expense",

                            Title = expense.Title,

                            Amount = expense.Amount,

                            Currency = group.Currency ?? "USD",

                            GroupId = group.Id,

                            GroupName = group.Name,

                            UserId = expense.PaidBy,

                            UserName = expense.PaidBy,

                            CreatedAt =
                                expense.CreatedAt
                        }
                    )
            )
            .OrderByDescending(activity =>
                activity.CreatedAt
            )
            .Take(25)
            .ToList();

        return activities;
    }
}