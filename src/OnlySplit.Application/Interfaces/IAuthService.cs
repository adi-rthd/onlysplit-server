using OnlySplit.Application.Features.Auth;

namespace OnlySplit.Application.Interfaces;

public interface IAuthService
{
    Task<UserResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> SignupAsync(SignupRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<UserResponse> GetMeAsync(CancellationToken cancellationToken = default);
}
