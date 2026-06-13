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
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default
    )
    {
        var userId = currentUser.UserId;

        var groups = await context.Groups
            .AsNoTracking()
            .Include(g => g.Members)
            .Include(g => g.Expenses)
                .ThenInclude(e => e.Splits)
            .Where(g =>
                g.Members.Any(m =>
                    m.UserId == userId
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
            .FirstOrDefault()?.Currency ?? "INR";

        /**
         * TOTAL MEMBERS
         */
        var totalMembers = groups.Sum(g =>
            g.Members.Count
        );

        /**
         * TOTAL SPENDING
         */
        var totalSpending = groups.Sum(g =>
            g.Expenses.Sum(e =>
                e.Amount
            )
        );

        decimal netBalance = 0;

        int youOweGroups = 0;
        int youAreOwedGroups = 0;

        /**
         * BALANCE CALCULATION
         *
         * Net Balance =
         * Total Paid By Me
         * -
         * Total Amount Owed By Me
         */
        foreach (var group in groups)
        {
            var groupPaid = group.Expenses
                .Where(e => e.PaidBy == userId)
                .Sum(e => e.Amount);

            var groupOwed = group.Expenses
                .SelectMany(e => e.Splits)
                .Where(s => s.UserId == userId)
                .Sum(s => s.AmountOwed);

            var groupNet = groupPaid - groupOwed;

            netBalance += groupNet;

            if (groupNet > 0)
            {
                youAreOwedGroups++;
            }
            else if (groupNet < 0)
            {
                youOweGroups++;
            }
        }

        decimal youOwe = 0;
        decimal youAreOwed = 0;

        if (netBalance > 0)
        {
            youAreOwed = netBalance;
        }
        else if (netBalance < 0)
        {
            youOwe = Math.Abs(netBalance);
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

                        Currency = group.Currency ?? "INR",

                        GroupName = group.Name,

                        PaidByName = expense.PaidBy,

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