namespace OnlySplit.Application.Features.Auth;

public sealed record SignupRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? AvatarUrl);
public sealed class UpdateProfileRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public string? UpiId { get; set; }
    public string? PreferredUpiApp { get; set; }
    public string? NotificationPreferencesJson { get; set; }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? AvatarUrl,
    string Role,
    DateTimeOffset CreatedAt,
    string? UpiId = null,
    string? PreferredUpiApp = null,
    string? NotificationPreferencesJson = null,
    DateTimeOffset? UpdatedAt = null);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    UserResponse User);

public sealed record UserSearchResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? AvatarUrl
);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);