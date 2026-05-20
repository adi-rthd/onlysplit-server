using Microsoft.EntityFrameworkCore;
using OnlySplit.Application.Analytics.Interfaces;
using OnlySplit.Application.Interfaces;
using OnlySplit.Infrastructure.Database;
namespace OnlySplit.Infrastructure.Services;

public sealed class AnalyticsService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser
) : IAnalyticsService
{
    // =========================
    // SPENDING TRENDS
    // =========================
    public async Task<object>
        GetSpendingTrendsAsync(
            CancellationToken cancellationToken = default
        )
    {
        var userId =
            currentUser.UserId;

        var expenses =
            await context.Expenses
                .AsNoTracking()
                .Include(x => x.Group)
                .Where(x =>
                    x.Group.Members.Any(m =>
                        m.UserId == userId
                    )
                )
                .ToListAsync(
                    cancellationToken
                );

        var trends = expenses
            .GroupBy(x => new
            {
                x.CreatedAt.Year,
                x.CreatedAt.Month
            })
            .OrderBy(x =>
                x.Key.Year
            )
            .ThenBy(x =>
                x.Key.Month
            )
            .Select(x => new
            {
                month =
                    $"{x.Key.Month:D2}/{x.Key.Year}",

                amount =
                    x.Sum(e =>
                        e.Amount
                    )
            })
            .ToList();

        return trends;
    }

    // =========================
    // CATEGORY BREAKDOWN
    // =========================
    public async Task<object>
        GetCategoryBreakdownAsync(
            CancellationToken cancellationToken = default
        )
    {
        var userId =
            currentUser.UserId;

        var expenses =
            await context.Expenses
                .AsNoTracking()
                .Include(x => x.Group)
                .Where(x =>
                    x.Group.Members.Any(m =>
                        m.UserId == userId
                    )
                )
                .ToListAsync(
                    cancellationToken
                );

        var breakdown = expenses
            .GroupBy(x =>
                string.IsNullOrWhiteSpace(
                    x.Category
                )
                    ? "Other"
                    : x.Category
            )
            .Select(x => new
            {
                category =
                    x.Key,

                amount =
                    x.Sum(e =>
                        e.Amount
                    )
            })
            .OrderByDescending(x =>
                x.amount
            )
            .ToList();

        return breakdown;
    }

    // =========================
    // GROUP BREAKDOWN
    // =========================
    public async Task<object>
        GetGroupBreakdownAsync(
            CancellationToken cancellationToken = default
        )
    {
        var userId =
            currentUser.UserId;

        var groups =
            await context.Groups
                .AsNoTracking()
                .Include(x => x.Members)
                .Include(x => x.Expenses)
                .Where(x =>
                    x.Members.Any(m =>
                        m.UserId == userId
                    )
                )
                .ToListAsync(
                    cancellationToken
                );

        var result = groups
            .Select(group => new
            {
                groupName =
                    group.Name,

                memberCount =
                    group.Members.Count,

                amount =
                    group.Expenses.Sum(
                        e => e.Amount
                    )
            })
            .OrderByDescending(x =>
                x.amount
            )
            .ToList();

        return result;
    }
}