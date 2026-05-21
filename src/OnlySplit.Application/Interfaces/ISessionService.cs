using OnlySplit.Infrastructure.Authentication.Redis;

namespace OnlySplit.Application.Features.Redis;

public interface ISessionService
{
    Task CreateSessionAsync(RedisSession session);

    Task<RedisSession?> GetSessionAsync(Guid sessionId);

    Task RevokeSessionAsync(Guid sessionId);

    Task DeleteSessionAsync(Guid sessionId);
}