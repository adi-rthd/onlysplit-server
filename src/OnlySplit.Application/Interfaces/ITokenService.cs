using OnlySplit.Application.Features.Auth;
using OnlySplit.Domain.Entities;

namespace OnlySplit.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    UserResponse ToUserResponse(User user);
}
