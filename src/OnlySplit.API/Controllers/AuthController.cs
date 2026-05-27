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
    
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateProfile( [FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {

        var response = await authService.UpdateProfileAsync(request, cancellationToken);

        return Ok(ApiResponse<Object>.Ok(response));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<Task<ApiResponse<object>>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, IpAddress, cancellationToken);
        Response.Cookies.Append(
            "refreshToken",
            response.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Domain = ".onlylabs.in",
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            }
        );

        return Ok(ApiResponse<object>.Ok(new
        {
            response.AccessToken,
            response.AccessTokenExpiresAt,
            response.User
        })
        );
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException(
                "Refresh token missing."
            );
        }

        var response = await authService.RefreshAsync(new RefreshTokenRequest(refreshToken), IpAddress, cancellationToken);
        Response.Cookies.Append(
            "refreshToken",
            response.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Domain = ".onlylabs.in",
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        return Ok(ApiResponse<object>.Ok(new
        {
            response.AccessToken,
            response.AccessTokenExpiresAt,
            response.User
        },
        "Token refreshed successfully."));
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
