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
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OnlySplit.Infrastructure.Services;

public sealed class AuthService(
    OnlySplitDbContext context,
    ITokenService tokenService,
    ICurrentUserService currentUser,
    IActivityService activityService,
    ISessionService sessionService,
    IEmailService emailService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    private static readonly Regex UpiIdRegex = new(
        @"^[a-zA-Z0-9.\-_]{2,256}@[a-zA-Z]{2,64}$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> ValidPreferredUpiApps = new(StringComparer.Ordinal)
    {
        "GooglePay", "PhonePe", "Paytm", "None"
    };

    private static readonly HashSet<string> NotificationPreferencesKeys = new(StringComparer.Ordinal)
    {
        "expenseAdded", "settlementRequested", "settlementConfirmed", "settlementRejected", "pushNotifications"
    };

    public async Task<UserResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        var user =
        await context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
        ?? throw new NotFoundException("User not found.");

        // Validate UPI ID if provided (null/empty is allowed for clearing)
        if (!string.IsNullOrEmpty(request.UpiId))
        {
            if (!UpiIdRegex.IsMatch(request.UpiId))
                throw new AppException("Invalid UPI ID format. Expected format: username@provider");
        }

        // Validate PreferredUpiApp if provided
        if (request.PreferredUpiApp is not null && !ValidPreferredUpiApps.Contains(request.PreferredUpiApp))
        {
            throw new AppException("Invalid preferred app. Allowed values: GooglePay, PhonePe, Paytm, None");
        }

        // Validate NotificationPreferencesJson if provided (null is allowed for clearing)
        if (request.NotificationPreferencesJson is not null)
        {
            ValidateNotificationPreferencesJson(request.NotificationPreferencesJson);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        user.UpiId = string.IsNullOrEmpty(request.UpiId) ? null : request.UpiId.Trim();
        user.PreferredUpiApp = request.PreferredUpiApp;
        user.NotificationPreferencesJson = request.NotificationPreferencesJson;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return new UserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.AvatarUrl,
            user.Role,
            user.CreatedAt,
            user.UpiId,
            user.PreferredUpiApp,
            user.NotificationPreferencesJson,
            user.UpdatedAt
        );
    }

    private static void ValidateNotificationPreferencesJson(string json)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw new AppException("Notification preferences must be valid JSON.");
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new AppException("Invalid notification preferences format.");
            }

            var properties = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (!NotificationPreferencesKeys.Contains(property.Name))
                {
                    throw new AppException("Invalid notification preferences format.");
                }

                if (property.Value.ValueKind != JsonValueKind.True && property.Value.ValueKind != JsonValueKind.False)
                {
                    throw new AppException("Invalid notification preferences format.");
                }

                properties.Add(property.Name);
            }

            if (properties.Count != NotificationPreferencesKeys.Count)
            {
                throw new AppException("Invalid notification preferences format.");
            }
        }
    }
    public async Task<AuthResponse> SignupAsync(SignupRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var exists =
        await context.Users.AnyAsync(user => user.Email == email, cancellationToken);

        if (exists)
            throw new ConflictException("A user with this email already exists.");

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

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(x => x.Id == currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        if (request.NewPassword.Length < 6)
        {
            throw new AppException("New password must be at least 6 characters.");
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
            return;

        var rawToken = tokenService.GenerateRefreshToken();

        user.PasswordResetTokenHash = tokenService.HashRefreshToken(rawToken);

        user.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(1);

        await context.SaveChangesAsync(cancellationToken);

        var resetLink =
            $"https://onlysplit.com/reset-password" +
            $"?token={Uri.EscapeDataString(rawToken)}" +
            $"&email={Uri.EscapeDataString(email)}";

        var html = $""" 
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="UTF-8">
        <title>Reset Your OnlySplit Password</title>
        </head>

        <body style="
        margin:0;
        padding:0;
        background:#050816;
        font-family:Inter,Segoe UI,sans-serif;
        ">

        <table width="100%" cellpadding="0" cellspacing="0">
        <tr>
        <td align="center" style="padding:40px 20px;">

        <table width="620" cellpadding="0" cellspacing="0"
        style="
        background:#0b1020;
        border:1px solid rgba(255,255,255,0.08);
        border-radius:24px;
        overflow:hidden;
        ">

        <tr>
        <td style="
        padding:50px;
        text-align:center;
        ">

        <img
        src="https://onlysplit.com/logo.png"
        alt="OnlySplit"
        width="180"
        style="margin-bottom:40px;" />

        <div style="
        display:inline-block;
        padding:10px 18px;
        border:1px solid rgba(255,255,255,0.08);
        border-radius:999px;
        color:#8b5cf6;
        font-size:12px;
        letter-spacing:2px;
        text-transform:uppercase;
        margin-bottom:24px;">
        Account Security
        </div>

        <h1 style="
        margin:0;
        font-size:52px;
        font-weight:800;
        line-height:1.05;
        color:#ffffff;">
        Reset your
        <span style="
        background:linear-gradient(90deg,#7c3aed,#60a5fa);
        -webkit-background-clip:text;
        -webkit-text-fill-color:transparent;">
        password
        </span>
        </h1>

        <p style="
        margin:28px auto;
        max-width:460px;
        font-size:18px;
        line-height:1.8;
        color:#94a3b8;">
        A request was made to reset your OnlySplit account password.
        If this was you, continue below.
        </p>

        <a href="{resetLink}"
        style="
        display:inline-block;
        padding:18px 36px;
        border-radius:14px;
        background:linear-gradient(90deg,#6d4aff,#5b8cff);
        color:white;
        text-decoration:none;
        font-size:18px;
        font-weight:700;">
        Reset Password → </a>

        <p style="
        margin-top:32px;
        font-size:14px;
        color:#64748b;">
        This secure link expires in 1 hour.
        </p>

        <hr style="
        border:none;
        height:1px;
        background:rgba(255,255,255,0.08);
        margin:40px 0;" />

        <p style="
        font-size:14px;
        line-height:1.8;
        color:#64748b;">
        If you didn't request a password reset,
        you can safely ignore this email.
        No changes will be made to your account.
        </p>

        </td>
        </tr>
        </table>

        <p style="
        margin-top:20px;
        color:#475569;
        font-size:12px;">
        © OnlySplit · Split expenses with precision
        </p>

        </td>
        </tr>
        </table>

        </body>
        </html>

        """;

        await emailService.SendAsync(email, "Reset your OnlySplit password", html, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new UnauthorizedAccessException("Reset token is required.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            throw new AppException("Password must be at least 6 characters.");

        var tokenHash = tokenService.HashRefreshToken(request.Token);

        var user = await context.Users.FirstOrDefaultAsync(
            u => u.PasswordResetTokenHash == tokenHash && u.PasswordResetExpiresAt > DateTime.UtcNow,
            cancellationToken
        ) ?? throw new UnauthorizedAccessException("Invalid or expired reset token.");

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiresAt = null;

        await context.SaveChangesAsync(cancellationToken);
    }
}
