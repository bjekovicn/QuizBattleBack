using QuizBattle.Application.Features.Users;

namespace QuizBattle.Application.Shared.Abstractions.Auth
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(UserResponse user);
        string GenerateRefreshToken();
        int? ValidateAccessToken(string token, bool validateLifetime = true);
        int GetRefreshTokenExpirationDays();
    }

    public sealed record TokenResponse(
        string AccessToken,
        string RefreshToken,
        long ExpiresAt);
}
