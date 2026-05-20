// Application/Analytics/Interfaces/IAnalyticsService.cs

namespace OnlySplit.Application.Analytics.Interfaces;

public interface IAnalyticsService
{
    Task<object> GetSpendingTrendsAsync(
        CancellationToken cancellationToken = default
    );

    Task<object> GetCategoryBreakdownAsync(
        CancellationToken cancellationToken = default
    );

    Task<object> GetGroupBreakdownAsync(
        CancellationToken cancellationToken = default
    );
}