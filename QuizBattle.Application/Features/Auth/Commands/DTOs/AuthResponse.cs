using QuizBattle.Application.Features.Users;

namespace QuizBattle.Application.Features.Auth.Commands.DTOs
{
    public sealed record AuthResponse(
        UserResponse User,
        string AccessToken,
        string RefreshToken,
        long ExpiresAt);
}
