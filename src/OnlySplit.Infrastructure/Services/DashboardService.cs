using Microsoft.EntityFrameworkCore;
using OnlySplit.Application.Dashboard.DTOs;
using OnlySplit.Application.Dashboard.Interfaces;
using OnlySplit.Application.Interfaces;
using OnlySplit.Infrastructure.Database;

namespace OnlySplit.Application.Dashboard.Services;

public class DashboardService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser
) : IDashboardService
{public async Task<DashboardSummaryResponse> GetSummaryAsync(
    CancellationToken cancellationToken = default
)
{
    var userId = currentUser.UserId;

    var groups = await context.Groups
        .AsNoTracking()
        .Include(group => group.Members)
        .Include(group => group.Expenses)
        .Where(group =>
            group.Members.Any(member =>
                member.UserId == userId
            )
        )
        .ToListAsync(cancellationToken);

    /**
     * TOTAL GROUPS
     */
    var totalGroups = groups.Count;

    /**
     * DASHBOARD CURRENCY
     */
    var currency = groups
        .FirstOrDefault()?.Currency ?? "USD";

    /**
     * TOTAL MEMBERS
     */
    var totalMembers = groups.Sum(group =>
        group.Members.Count
    );

    /**
     * TOTAL SPENDING
     */
    var totalSpending = groups.Sum(group =>
        group.Expenses.Sum(expense =>
            expense.Amount
        )
    );

    decimal youOwe = 0;
    decimal youAreOwed = 0;

    int youOweGroups = 0;
    int youAreOwedGroups = 0;

    /**
     * CALCULATE BALANCES
     */
    foreach (var group in groups)
    {
        decimal groupOwe = 0;
        decimal groupOwed = 0;

        var memberCount = Math.Max(
            group.Members.Count,
            1
        );

        foreach (var expense in group.Expenses)
        {
            var splitAmount =
                expense.Amount / memberCount;

            if (expense.PaidBy == userId)
            {
                var owedAmount =
                    expense.Amount - splitAmount;

                youAreOwed += owedAmount;

                groupOwed += owedAmount;
            }
            else
            {
                youOwe += splitAmount;

                groupOwe += splitAmount;
            }
        }

        if (groupOwe > 0)
        {
            youOweGroups++;
        }

        if (groupOwed > 0)
        {
            youAreOwedGroups++;
        }
    }

    /**
     * RECENT ACTIVITIES
     */
    var recentActivities = groups
        .SelectMany(group =>
            group.Expenses.Select(expense =>
                new RecentActivityResponse
                {
                    ExpenseId = expense.Id,

                    Title = expense.Title,

                    Amount = expense.Amount,

                    Currency = group.Currency ?? "USD",

                    GroupName = group.Name,

                    // fallback until navigation property exists
                    PaidByName = expense.PaidBy,

                    // use existing timestamp property
                    CreatedAt = expense.CreatedAt
                }
            )
        )
        .OrderByDescending(expense =>
            expense.CreatedAt
        )
        .Take(5)
        .ToList();

    return new DashboardSummaryResponse
    {
        Currency = currency,

        TotalGroups = totalGroups,

        TotalMembers = totalMembers,

        TotalSpending = Math.Round(
            totalSpending,
            2
        ),

        YouOwe = Math.Round(
            youOwe,
            2
        ),

        YouOweGroups = youOweGroups,

        YouAreOwed = Math.Round(
            youAreOwed,
            2
        ),

        YouAreOwedGroups = youAreOwedGroups,

        RecentActivities = recentActivities
    };
}
}