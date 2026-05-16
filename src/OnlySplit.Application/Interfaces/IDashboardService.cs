using OnlySplit.Application.Dashboard.DTOs;

namespace OnlySplit.Application.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default
    );
}