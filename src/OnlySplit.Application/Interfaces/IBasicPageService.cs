using OnlySplit.Application.Features.BasicPage;

namespace OnlySplit.Application.Interfaces;

public interface IBasicPageService
{
    Task<LandingStatsResponse> GetLandingStatsAsync(
        CancellationToken cancellationToken = default
    );
}