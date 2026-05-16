using Microsoft.EntityFrameworkCore;
using OnlySplit.Application.Features.BasicPage;
using OnlySplit.Application.Interfaces;
using OnlySplit.Infrastructure.Database;

namespace OnlySplit.Infrastructure.Services;

public sealed class BasicPageService(
    OnlySplitDbContext context
) : IBasicPageService
{
    public async Task<LandingStatsResponse> GetLandingStatsAsync(
        CancellationToken cancellationToken = default)
    {
        return new LandingStatsResponse
        {
            RegisteredUsers = await context.Users.CountAsync(cancellationToken),

            ActiveGroups = await context.Groups.CountAsync(cancellationToken),

            ExpensesProcessed = await context.Expenses.CountAsync(cancellationToken)
        };
    }
}