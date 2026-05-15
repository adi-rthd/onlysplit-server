using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.Infrastructure.Authentication;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId)
                ? userId
                : throw new UnauthorizedAccessException("Authenticated user id is missing.");
        }
    }

    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public string? Role => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
