using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlySplit.Infrastructure.Authentication;
using OnlySplit.Domain.Constants;
using OnlySplit.Infrastructure.Database;
using OnlySplit.Application.Features.Auth;
using OnlySplit.Domain.Entities;
using OnlySplit.Domain.Exceptions;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.Infrastructure.Services;

public sealed class AuthService(
    OnlySplitDbContext context,
    ITokenService tokenService,
    ICurrentUserService currentUser,
    IActivityService activityService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponse> SignupAsync(SignupRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var exists = await context.Users.AnyAsync(user => user.Email == email, cancellationToken);
        if (exists)
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim()
        };

        context.Users.Add(user);
        var response = IssueTokens(user, ipAddress);
        await context.SaveChangesAsync(cancellationToken);

        await activityService.LogAsync(user.Id, ActivityTypes.UserSignedUp, new { user.Id, user.Email }, cancellationToken);
        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await context.Users.FirstOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var response = IssueTokens(user, ipAddress);
        await context.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var refreshToken = await context.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (refreshToken?.User is null || !refreshToken.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
        }

        var newRefreshToken = tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = tokenService.HashRefreshToken(newRefreshToken);

        refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReplacedByTokenHash = newRefreshTokenHash;

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = refreshToken.UserId,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ipAddress
        });

        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(refreshToken.User);
        await context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, newRefreshToken, expiresAt, tokenService.ToUserResponse(refreshToken.User));
    }

    public async Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var refreshToken = await context.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return;
        }

        if (refreshToken.UserId != currentUser.UserId)
        {
            throw new ForbiddenException("You cannot revoke another user's refresh token.");
        }

        refreshToken.RevokedAt ??= DateTimeOffset.UtcNow;
        refreshToken.RevokedByIp ??= ipAddress;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserResponse> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(candidate => candidate.Id == currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Authenticated user was not found.");

        return tokenService.ToUserResponse(user);
    }

    private AuthResponse IssueTokens(User user, string? ipAddress)
    {
        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ipAddress
        });

        return new AuthResponse(accessToken, refreshToken, expiresAt, tokenService.ToUserResponse(user));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    public async Task<IReadOnlyCollection<UserSearchResponse>> SearchUsersAsync(
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
