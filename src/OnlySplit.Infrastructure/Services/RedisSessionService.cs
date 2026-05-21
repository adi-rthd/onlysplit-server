using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OnlySplit.Application.Features.Redis;
using OnlySplit.Infrastructure.Authentication.Redis;
using StackExchange.Redis;

namespace OnlySplit.Infrastructure.Auth;

public class RedisSessionService : ISessionService
{
    private readonly IDatabase _database;
    private readonly string _prefix;

    public RedisSessionService(IConnectionMultiplexer redis, IConfiguration configuration)
    {
        _database = redis.GetDatabase();
        _prefix = configuration["Redis:InstanceName"] ?? "OnlySplit:";
    }

    private string GetKey(Guid sessionId)
        => $"{_prefix}session:{sessionId}";

    public async Task CreateSessionAsync(RedisSession session)
    {
        var key = GetKey(session.SessionId);

        var json = JsonSerializer.Serialize(session);

        var expiry = session.ExpiresAtUtc - DateTime.UtcNow;

        await _database.StringSetAsync(
            key,
            json,
            expiry
        );
    }

    public async Task<RedisSession?> GetSessionAsync(Guid sessionId)
    {
        var key = GetKey(sessionId);

        var value = await _database.StringGetAsync(key);

        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<RedisSession>(value.ToString());
    }

    public async Task RevokeSessionAsync(Guid sessionId)
    {
        var session = await GetSessionAsync(sessionId);

        if (session is null)
            return;

        session.Revoked = true;

        await CreateSessionAsync(session);
    }

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        var key = GetKey(sessionId);

        await _database.KeyDeleteAsync(key);
    }
}