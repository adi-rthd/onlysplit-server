// IActivityFeedService.cs

using OnlySplit.Application.Activities.DTOs;

namespace OnlySplit.Application.Activities.Interfaces;

public interface IActivityFeedService
{
    Task<IReadOnlyCollection<ActivityResponse>>
        GetActivitiesAsync(
            string scope,
            CancellationToken cancellationToken = default
        );
}