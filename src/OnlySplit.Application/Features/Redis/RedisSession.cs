namespace OnlySplit.Infrastructure.Authentication.Redis;

public class RedisSession
{
    public Guid SessionId { get; set; }

    public Guid UserId { get; set; }

    public string RefreshTokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public bool Revoked { get; set; }

    public string? Device { get; set; }

    public string? IpAddress { get; set; }
}