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
    DateTimeOffset CreatedAt);

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