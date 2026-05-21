using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlySplit.Infrastructure.Authentication;
using OnlySplit.Domain.Constants;
using OnlySplit.Infrastructure.Database;
using OnlySplit.Application.Features.Auth;
using OnlySplit.Domain.Entities;
using OnlySplit.Domain.Exceptions;
using OnlySplit.Application.Interfaces;
using OnlySplit.Application.Features.Redis;
using OnlySplit.Infrastructure.Authentication.Redis;

namespace OnlySplit.Infrastructure.Services;

public sealed class AuthService(
    OnlySplitDbContext context,
    ITokenService tokenService,
    ICurrentUserService currentUser,
    IActivityService activityService,
    ISessionService sessionService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponse> SignupAsync(
        SignupRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var exists = await context.Users
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (exists)
        {
            throw new ConflictException(
                "A user with this email already exists."
            );
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl)
                ? null
                : request.AvatarUrl.Trim()
        };

        context.Users.Add(user);

        var response = await IssueTokensAsync(user, ipAddress);

        await context.SaveChangesAsync(cancellationToken);

        await activityService.LogAsync(
            user.Id,
            ActivityTypes.UserSignedUp,
            new { user.Id, user.Email },
            cancellationToken
        );

        return response;
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var user = await context.Users
            .FirstOrDefaultAsync(
                candidate => candidate.Email == email,
                cancellationToken
            );

        if (
            user is null ||
            !PasswordHasher.Verify(request.Password, user.PasswordHash)
        )
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password."
            );
        }

        var response = await IssueTokensAsync(user, ipAddress);

        await context.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var parts = request.RefreshToken.Split('.');

        if (parts.Length != 2)
        {
            throw new UnauthorizedAccessException(
                "Refresh token is invalid."
            );
        }

        if (!Guid.TryParse(parts[0], out var sessionId))
        {
            throw new UnauthorizedAccessException(
                "Refresh token is invalid."
            );
        }

        var tokenSecret = parts[1];

        var session = await sessionService.GetSessionAsync(sessionId);

        if (session is null || session.Revoked)
        {
            throw new UnauthorizedAccessException(
                "Refresh token is invalid or expired."
            );
        }

        if (session.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await sessionService.DeleteSessionAsync(sessionId);

            throw new UnauthorizedAccessException(
                "Refresh token has expired."
            );
        }

        var incomingHash = tokenService.HashRefreshToken(tokenSecret);

        if (incomingHash != session.RefreshTokenHash)
        {
            throw new UnauthorizedAccessException(
                "Refresh token is invalid."
            );
        }

        var user = await context.Users
            .FirstOrDefaultAsync(
                candidate => candidate.Id == session.UserId,
                cancellationToken
            );

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "User not found."
            );
        }

        var newSecret = tokenService.GenerateRefreshToken();

        var newRefreshToken = $"{sessionId}.{newSecret}";

        session.RefreshTokenHash = tokenService
            .HashRefreshToken(newSecret);

        session.ExpiresAtUtc = DateTime.UtcNow
            .AddDays(_jwtOptions.RefreshTokenDays);

        await sessionService.CreateSessionAsync(session);

        var (accessToken, expiresAt) = tokenService
            .GenerateAccessToken(user);

        return new AuthResponse(
            accessToken,
            newRefreshToken,
            expiresAt,
            tokenService.ToUserResponse(user)
        );
    }

    public async Task LogoutAsync(
        LogoutRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var parts = request.RefreshToken.Split('.');

        if (parts.Length != 2)
        {
            return;
        }

        if (!Guid.TryParse(parts[0], out var sessionId))
        {
            return;
        }

        var session = await sessionService
            .GetSessionAsync(sessionId);

        if (session is null)
        {
            return;
        }

        if (session.UserId != currentUser.UserId)
        {
            throw new ForbiddenException(
                "You cannot revoke another user's refresh token."
            );
        }

        await sessionService.DeleteSessionAsync(sessionId);
    }

    public async Task<UserResponse> GetMeAsync(
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(
                candidate => candidate.Id == currentUser.UserId,
                cancellationToken
            )
            ?? throw new NotFoundException(
                "Authenticated user was not found."
            );

        return tokenService.ToUserResponse(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user,
        string? ipAddress)
    {
        var (accessToken, expiresAt) = tokenService
            .GenerateAccessToken(user);

        var sessionId = Guid.NewGuid();

        var refreshSecret = tokenService
            .GenerateRefreshToken();

        var refreshToken = $"{sessionId}.{refreshSecret}";

        var session = new RedisSession
        {
            SessionId = sessionId,
            UserId = user.Id,
            RefreshTokenHash = tokenService
                .HashRefreshToken(refreshSecret),
            ExpiresAtUtc = DateTime.UtcNow
                .AddDays(_jwtOptions.RefreshTokenDays),
            Revoked = false,
            IpAddress = ipAddress
        };

        await sessionService.CreateSessionAsync(session);

        return new AuthResponse(
            accessToken,
            refreshToken,
            expiresAt,
            tokenService.ToUserResponse(user)
        );
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    public async Task<IReadOnlyCollection<UserSearchResponse>>
        SearchUsersAsync(
            string query,
            CancellationToken cancellationToken = default)
    {
        query = query.Trim().ToLower();

        return await context.Users
            .AsNoTracking()
            .Where(x =>
                x.Id != currentUser.UserId &&
                (
                    x.Email.ToLower().Contains(query) ||
                    x.FirstName.ToLower().Contains(query) ||
                    x.LastName.ToLower().Contains(query)
                ))
            .OrderBy(x => x.FirstName)
            .Take(10)
            .Select(x => new UserSearchResponse(
                x.Id,
                x.FirstName,
                x.LastName,
                x.Email,
                x.AvatarUrl
            ))
            .ToListAsync(cancellationToken);
    }
}
