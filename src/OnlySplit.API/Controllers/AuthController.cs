using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlySplit.Application.Features.Auth;
using OnlySplit.Shared.Responses;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Signup(SignupRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.SignupAsync(request, IpAddress, cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(response, "Signup completed successfully."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, IpAddress, cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(response, "Login completed successfully."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request, IpAddress, cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(response, "Token refreshed successfully."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, IpAddress, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Logged out successfully."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Me(CancellationToken cancellationToken)
    {
        var response = await authService.GetMeAsync(cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(response));
    }

    private string? IpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserSearchResponse>>>> Search(
    [FromQuery] string q,
    CancellationToken cancellationToken)
    {
        var response = await authService.SearchUsersAsync(q, cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<UserSearchResponse>>
            .Ok(response));
    }
}
