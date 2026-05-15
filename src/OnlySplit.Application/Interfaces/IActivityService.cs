namespace OnlySplit.Application.Interfaces;

public interface IActivityService
{
    Task LogAsync(Guid userId, string type, object metadata, CancellationToken cancellationToken = default);
}
