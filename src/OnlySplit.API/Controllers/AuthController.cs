using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlySplit.Application.Features.Auth;
using OnlySplit.Shared.Responses;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, IWebHostEnvironment env) : ControllerBase
{
    private CookieOptions RefreshCookieOptions => new()
    {
        HttpOnly = true,
        Secure = !env.IsDevelopment(),
        SameSite = env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
        Domain = env.IsDevelopment() ? null : ".onlylabs.in",
        Path = "/",
        Expires = DateTimeOffset.UtcNow.AddDays(30)
    };

    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Signup(SignupRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.SignupAsync(request, IpAddress, cancellationToken);
        Response.Cookies.Append("refreshToken", response.RefreshToken, RefreshCookieOptions);

        return Ok(ApiResponse<object>.Ok(new
        {
            response.AccessToken,
            response.AccessTokenExpiresAt,
            response.RefreshToken,
            response.User
        }, "Signup completed successfully."));
    }
    
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {

        var response = await authService.UpdateProfileAsync(request, cancellationToken);

        return Ok(ApiResponse<Object>.Ok(response));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<Task<ApiResponse<object>>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, IpAddress, cancellationToken);
        Response.Cookies.Append("refreshToken", response.RefreshToken, RefreshCookieOptions);

        return Ok(ApiResponse<object>.Ok(new
        {
            response.AccessToken,
            response.AccessTokenExpiresAt,
            response.RefreshToken,
            response.User
        })
        );
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Refresh([FromBody] RefreshTokenRequest? body, CancellationToken cancellationToken)
    {
        // Body-first (Capacitor/mobile), cookie fallback (browser)
        var refreshToken = body?.RefreshToken;

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            refreshToken = Request.Cookies["refreshToken"];
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException(
                "Refresh token missing."
            );
        }

        var response = await authService.RefreshAsync(new RefreshTokenRequest(refreshToken), IpAddress, cancellationToken);
        Response.Cookies.Append("refreshToken", response.RefreshToken, RefreshCookieOptions);
        return Ok(ApiResponse<object>.Ok(new
        {
            response.AccessToken,
            response.AccessTokenExpiresAt,
            response.RefreshToken,
            response.User
        },
        "Token refreshed successfully."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequest? body, CancellationToken cancellationToken)
    {
        // Body-first (Capacitor/mobile), cookie fallback (browser)
        var refreshToken = body?.RefreshToken;

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            refreshToken = Request.Cookies["refreshToken"];
        }

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await authService.LogoutAsync(new LogoutRequest(refreshToken), IpAddress, cancellationToken);
        }

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Domain = env.IsDevelopment() ? null : ".onlylabs.in",
            Path = "/"
        });

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

    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Password changed successfully."));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "If that email exists, a reset link has been sent."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Password reset successfully."));
    }
}
